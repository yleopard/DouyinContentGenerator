using Microsoft.AspNetCore.SignalR;

namespace DouyinContentGenerator.API.Hubs;

public class GenerationHub : Hub
{
    private static readonly Dictionary<string, HashSet<string>> TaskSubscriptions = new();

    public async Task SubscribeToTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task_{taskId}");

        lock (TaskSubscriptions)
        {
            if (!TaskSubscriptions.ContainsKey(Context.ConnectionId))
                TaskSubscriptions[Context.ConnectionId] = new HashSet<string>();
            TaskSubscriptions[Context.ConnectionId].Add(taskId);
        }
    }

    public async Task UnsubscribeFromTask(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task_{taskId}");

        lock (TaskSubscriptions)
        {
            if (TaskSubscriptions.ContainsKey(Context.ConnectionId))
                TaskSubscriptions[Context.ConnectionId].Remove(taskId);
        }
    }

    public async Task ResubscribeToTasks()
    {
        lock (TaskSubscriptions)
        {
            if (TaskSubscriptions.TryGetValue(Context.ConnectionId, out var tasks))
            {
                foreach (var taskId in tasks)
                    Groups.AddToGroupAsync(Context.ConnectionId, $"task_{taskId}");
            }
        }

        await Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (TaskSubscriptions)
        {
            TaskSubscriptions.Remove(Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
