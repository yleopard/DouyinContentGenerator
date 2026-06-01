using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/image-templates")]
[Authorize]
public class ImageTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ImageTemplatesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ImageTemplate>>> GetTemplates([FromQuery] string? category = null)
    {
        var query = _db.ImageTemplates.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(t => t.Category == category);

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ImageTemplate>> GetTemplate(Guid id)
    {
        var template = await _db.ImageTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<ImageTemplate>> CreateTemplate([FromBody] ImageTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _db.ImageTemplates.Add(template);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ImageTemplate>> UpdateTemplate(Guid id, [FromBody] ImageTemplate update)
    {
        var template = await _db.ImageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        if (template.IsBuiltin && update.IsBuiltin)
            return BadRequest(new { error = "Cannot modify built-in templates" });

        template.Name = update.Name;
        template.Category = update.Category;
        template.Description = update.Description;
        template.PromptTemplate = update.PromptTemplate;
        template.Style = update.Style;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(template);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTemplate(Guid id)
    {
        var template = await _db.ImageTemplates.FindAsync(id);
        if (template == null) return NotFound();
        if (template.IsBuiltin) return BadRequest(new { error = "Cannot delete built-in templates" });

        _db.ImageTemplates.Remove(template);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
