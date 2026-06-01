using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/copywriting-templates")]
[Authorize]
public class CopywritingTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CopywritingTemplatesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CopywritingTemplate>>> GetTemplates([FromQuery] string? type = null)
    {
        var query = _db.CopywritingTemplates.AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.TemplateType == type);

        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CopywritingTemplate>> GetTemplate(Guid id)
    {
        var template = await _db.CopywritingTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<CopywritingTemplate>> CreateTemplate([FromBody] CopywritingTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;

        _db.CopywritingTemplates.Add(template);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CopywritingTemplate>> UpdateTemplate(Guid id, [FromBody] CopywritingTemplate update)
    {
        var template = await _db.CopywritingTemplates.FindAsync(id);
        if (template == null) return NotFound();

        if (template.IsBuiltin && update.IsBuiltin)
            return BadRequest(new { error = "Cannot modify built-in templates" });

        template.Name = update.Name;
        template.TemplateType = update.TemplateType;
        template.Content = update.Content;
        template.Variables = update.Variables;

        await _db.SaveChangesAsync();
        return Ok(template);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTemplate(Guid id)
    {
        var template = await _db.CopywritingTemplates.FindAsync(id);
        if (template == null) return NotFound();
        if (template.IsBuiltin) return BadRequest(new { error = "Cannot delete built-in templates" });

        _db.CopywritingTemplates.Remove(template);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
