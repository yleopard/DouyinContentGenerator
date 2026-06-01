using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IGenerationTaskService
{
    Task<GenerationTaskResponse> CreateTaskAsync(Guid userId, CreateGenerationTaskRequest request);
    Task<GenerationTaskResponse?> GetTaskAsync(Guid userId, Guid taskId);
    Task<List<GenerationTaskResponse>> GetTasksAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null);
    Task<bool> CancelTaskAsync(Guid userId, Guid taskId);
    Task<List<GeneratedImageResponse>> GetGeneratedImagesAsync(Guid taskId, Guid? templateId = null);
    Task<List<GeneratedTextResponse>> GetGeneratedTextsAsync(Guid taskId, Guid? templateId = null);
    Task<bool> SelectContentAsync(Guid taskId, SelectContentRequest request);
}
