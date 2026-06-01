namespace DouyinContentGenerator.Core.Models;

public class TaskImageTemplate
{
    public Guid TaskId { get; set; }
    public Guid ImageTemplateId { get; set; }

    // Navigation properties
    public GenerationTask Task { get; set; } = null!;
    public ImageTemplate ImageTemplate { get; set; } = null!;
}
