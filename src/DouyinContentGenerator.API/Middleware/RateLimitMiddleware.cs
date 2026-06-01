using System.Collections.Concurrent;

namespace DouyinContentGenerator.API.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RateLimitMiddleware(RequestDelegate next, int maxRequests = 100, int windowMinutes = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _window = TimeSpan.FromMinutes(windowMinutes);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(_maxRequests, _window));

            if (!bucket.TryConsume())
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Rate limit exceeded\"}");
                return;
            }
        }

        await _next(context);
    }

    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly TimeSpan _window;
        private int _tokens;
        private DateTime _lastRefill;

        public TokenBucket(int capacity, TimeSpan window)
        {
            _capacity = capacity;
            _window = window;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryConsume()
        {
            Refill();
            if (_tokens > 0)
            {
                Interlocked.Decrement(ref _tokens);
                return true;
            }
            return false;
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            if (now - _lastRefill > _window)
            {
                _tokens = _capacity;
                _lastRefill = now;
            }
        }
    }
}
