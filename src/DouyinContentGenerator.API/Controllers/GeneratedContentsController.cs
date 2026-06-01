using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/generated-contents")]
[Authorize]
public class GeneratedContentsController : ControllerBase
{
    private readonly IGenerationTaskService _taskService;

    public GeneratedContentsController(IGenerationTaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("images")]
    public async Task<ActionResult<List<GeneratedImageResponse>>> GetImages(
        [FromQuery] Guid taskId,
        [FromQuery] Guid? templateId = null)
    {
        var images = await _taskService.GetGeneratedImagesAsync(taskId, templateId);
        return Ok(images);
    }

    [HttpGet("texts")]
    public async Task<ActionResult<List<GeneratedTextResponse>>> GetTexts(
        [FromQuery] Guid taskId,
        [FromQuery] Guid? templateId = null)
    {
        var texts = await _taskService.GetGeneratedTextsAsync(taskId, templateId);
        return Ok(texts);
    }

    [HttpPost("{taskId}/select")]
    public async Task<ActionResult> SelectContent(Guid taskId, [FromBody] SelectContentRequest request)
    {
        var success = await _taskService.SelectContentAsync(taskId, request);
        if (!success) return NotFound();
        return Ok();
    }
}
