using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/generation-tasks")]
[Authorize]
public class GenerationTasksController : ControllerBase
{
    private readonly IGenerationTaskService _taskService;

    public GenerationTasksController(IGenerationTaskService taskService)
    {
        _taskService = taskService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException());
    }

    [HttpPost]
    public async Task<ActionResult<GenerationTaskResponse>> CreateTask([FromBody] CreateGenerationTaskRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var task = await _taskService.CreateTaskAsync(userId, request);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<List<GenerationTaskResponse>>> CreateBatchTasks([FromBody] BatchGenerationRequest request)
    {
        var userId = GetCurrentUserId();
        var tasks = new List<GenerationTaskResponse>();

        foreach (var taskReq in request.Tasks)
        {
            try
            {
                var task = await _taskService.CreateTaskAsync(userId, taskReq);
                tasks.Add(task);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, completedTasks = tasks });
            }
        }

        return Ok(tasks);
    }

    [HttpGet]
    public async Task<ActionResult<List<GenerationTaskResponse>>> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var userId = GetCurrentUserId();
        var tasks = await _taskService.GetTasksAsync(userId, page, pageSize, status);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GenerationTaskResponse>> GetTask(Guid id)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.GetTaskAsync(userId, id);

        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> CancelTask(Guid id)
    {
        var userId = GetCurrentUserId();
        var cancelled = await _taskService.CancelTaskAsync(userId, id);

        if (!cancelled) return BadRequest(new { error = "Task already finished or not found" });
        return Ok();
    }
}
