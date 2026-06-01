# 抖音图文带货AI生成系统设计文档

**版本:** 2.0  
**日期:** 2026-05-29  
**状态:** 已审核

---

## 一、项目概述

### 1.1 项目背景

开发一套AI自动生成抖音图文带货内容的系统,面向小团队/工作室用户,支持批量生成产品场景图和带货文案,满足中等规模(每天50-200套)的内容生产需求。

### 1.2 核心目标

- **自动化生成:** 上传产品信息后,自动生成多套图文组合
- **参考图生成:** 支持上传产品实拍图,AI基于参考图生成场景化图片
- **模板化生成:** 支持预定义图片模板(厨房/客厅/卧室等场景),一键批量生成
- **批量处理:** 支持并发生成,提高生产效率
- **灵活配置:** AI服务提供商可插拔切换,避免供应商锁定
- **成本控制:** 实时监控API调用成本,防止超预算
- **易用性:** 低代码偏好,界面直观,快速上手

### 1.3 目标用户

- 抖音带货小团队/工作室
- 电商内容创作者
- 需要批量生产带货图文的商家

---

## 二、技术架构

### 2.1 技术栈

**后端:**
- .NET 8 Web API
- Entity Framework Core + PostgreSQL
- Hangfire (后台任务队列)
- SignalR (实时通信)
- Serilog (日志)
- Polly (弹性调用)
- Redis (预算预留/缓存)

**前端:**
- React 18 + TypeScript
- Vite (构建工具)
- Mantine UI (组件库)
- Zustand (状态管理)
- React Query (数据获取)
- SignalR Client (实时推送,自动重连)

**AI服务:**
- 图片生成: 阿里云通义万相 (可替换为文心一格等)
- 文案生成: 阿里云通义千问 (可替换为文心一言等)
- 插件化架构,支持动态切换提供商
- 统一成本计算接口

**基础设施:**
- Supabase Storage (对象存储)
- Docker + Docker Compose (容器化部署)
- PostgreSQL 自动备份至 Supabase Storage

### 2.2 系统架构图

```
┌──────────────────────────────────────────────┐
│          前端层 (React + TypeScript)          │
│  - 产品管理界面 (含图片上传组件)               │
│  - 批量生成配置 (模板选择+预览)                │
│  - 内容预览与筛选 (按模板分组)                 │
│  - AI服务配置                                 │
│  - 数据统计看板                               │
└────────────────┬─────────────────────────────┘
                 │ RESTful API + SignalR
┌────────────────▼─────────────────────────────┐
│         API层 (.NET 8 Web API)                │
│  - Controllers (REST endpoints)               │
│  - JWT Authentication + 用户隔离              │
│  - Request Validation                         │
│  - Rate Limiting                              │
│  - Budget Guard Middleware                    │
└────────────────┬─────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────┐
│       业务逻辑层 (Core Services)              │
│  - GenerationTaskService (主/子任务调度)      │
│  - ContentWorkflowService                     │
│  - CostMonitoringService                      │
│  - BudgetReservationService                   │
│  - TemplateManagementService                  │
└────────────────┬─────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────┐
│      AI服务抽象层 (Interfaces)                │
│  - IImageGenerator (支持CancellationToken)    │
│  - ITextGenerator (支持CancellationToken)     │
│  - ICostCalculator                            │
└────────────────┬─────────────────────────────┘
                 │
     ┌───────────┼───────────┐
     │           │           │
┌────▼────┐ ┌───▼────┐ ┌───▼──────┐
│通义万相  │ │文心一格 │ │其他提供商 │
│插件     │ │插件    │ │插件      │
└─────────┘ └────────┘ └──────────┘
                 │
┌────────────────▼─────────────────────────────┐
│       基础设施层 (Infrastructure)             │
│  - EF Core + PostgreSQL                       │
│  - Hangfire (后台任务)                        │
│  - Redis (预算预留/缓存)                      │
│  - Supabase Storage                           │
│  - Serilog + 结构化日志                       │
│  - Data Protection API (密钥加密)             │
└──────────────────────────────────────────────┘
```

### 2.3 设计原则

1. **插件化架构:** AI服务通过接口抽象,不同提供商作为插件实现,通过配置动态切换
2. **异步处理:** 耗时操作通过Hangfire子任务拆分处理,支持并行执行
3. **实时反馈:** SignalR推送详细任务进度,前端自动重连
4. **成本控制:** 预算预留机制,防止执行中超额;成本精确到每次调用
5. **可扩展性:** 新增AI提供商只需实现接口并注册,无需修改业务代码

---

## 三、数据库设计

### 3.1 ER图

```
users --< user_roles >-- roles

products (N:1) users
products (1) --< product_images
products (1) --< generation_tasks
generation_tasks (1) --< task_image_templates
generation_tasks (1) --< generated_images
generation_tasks (1) --< generated_texts
generated_images (N:1) image_templates
generated_texts (N:1) copywriting_templates

cost_records_summary (物化视图/每日聚合)
```

### 3.2 表结构

#### users (用户表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| username | VARCHAR(100) | 用户名 |
| email | VARCHAR(200) | 邮箱 |
| password_hash | VARCHAR(500) | 密码哈希 |
| is_active | BOOLEAN | 是否激活 |
| created_at | TIMESTAMP | 创建时间 |

#### roles (角色表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| name | VARCHAR(50) | 角色名 (Admin/Operator) |

#### user_roles (用户角色关联)

| 字段 | 类型 | 说明 |
|------|------|------|
| user_id | UUID | 用户ID |
| role_id | UUID | 角色ID |

#### products (产品表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | UUID | 所属用户ID |
| name | VARCHAR(200) | 产品名称 |
| category | VARCHAR(100) | 产品分类 |
| description | TEXT | 产品描述 |
| selling_points | TEXT[] | 卖点数组 |
| price | DECIMAL(10,2) | 价格 |
| tags | TEXT[] | 标签数组 |
| generation_config | JSONB | 生成配置(风格、场景偏好等) |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

#### product_images (产品图片表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| product_id | UUID | 产品ID |
| url | TEXT | 图片URL |
| type | VARCHAR(20) | 类型: product / reference |
| order | INT | 排序 |
| created_at | TIMESTAMP | 上传时间 |

#### generation_tasks (生成任务表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | UUID | 用户ID |
| product_id | UUID | 关联产品ID |
| status | VARCHAR(50) | pending/processing/completed/failed/cancelled |
| progress | INT | 进度百分比 (0-100) |
| status_message | TEXT | 状态消息 |
| image_count | INT | 每模板生成图片数 |
| text_variants_count | INT | 每模板文案变体数 |
| use_reference_image | BOOLEAN | 是否启用图生图 |
| error_message | TEXT | 错误信息 |
| estimated_cost | DECIMAL(10,4) | 预估总成本 |
| actual_cost | DECIMAL(10,4) | 实际成本 |
| started_at | TIMESTAMP | 开始时间 |
| completed_at | TIMESTAMP | 完成时间 |
| created_at | TIMESTAMP | 创建时间 |

#### task_image_templates (任务-图片模板关联)

| 字段 | 类型 | 说明 |
|------|------|------|
| task_id | UUID | 任务ID |
| image_template_id | UUID | 图片模板ID |

#### generated_images (生成图片表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| task_id | UUID | 任务ID |
| image_template_id | UUID | 图片模板ID |
| image_url | TEXT | 生成图片URL |
| cost | DECIMAL(10,4) | 本次调用成本 |
| status | VARCHAR(20) | success/failed |
| is_selected | BOOLEAN | 是否被用户选中 |
| error_message | TEXT | 失败原因 |
| created_at | TIMESTAMP | 创建时间 |

#### generated_texts (生成文案表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| task_id | UUID | 任务ID |
| text_template_id | UUID | 文案模板ID |
| content | TEXT | 文案内容 |
| cost | DECIMAL(10,4) | 成本 |
| status | VARCHAR(20) | success/failed |
| is_selected | BOOLEAN | 是否选中 |
| error_message | TEXT | 失败原因 |
| created_at | TIMESTAMP | 创建时间 |

#### ai_provider_configs (AI服务配置表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| provider_type | VARCHAR(50) | image_generation / text_generation |
| provider_name | VARCHAR(100) | 提供商名称 |
| is_active | BOOLEAN | 是否激活 |
| config_data | JSONB | 加密配置 (API密钥等) |
| usage_stats | JSONB | 使用统计 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

#### cost_records_summary (成本汇总视图)

| 字段 | 类型 | 说明 |
|------|------|------|
| date | DATE | 日期 |
| user_id | UUID | 用户ID |
| provider_name | VARCHAR(100) | 提供商 |
| service_type | VARCHAR(50) | image / text |
| call_count | INT | 调用次数 |
| total_cost | DECIMAL(10,4) | 总成本 |

> 原始成本不再单独建表存储,直接通过 generated_images 和 generated_texts 的 cost 字段聚合。

#### copywriting_templates (文案模板表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| name | VARCHAR(200) | 模板名称 |
| template_type | VARCHAR(50) | 模板类型 |
| content | TEXT | 模板内容 |
| variables | TEXT[] | 变量列表 |
| is_builtin | BOOLEAN | 是否内置 |
| created_at | TIMESTAMP | 创建时间 |

#### image_templates (图片模板表)

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| name | VARCHAR(200) | 模板名称 |
| category | VARCHAR(100) | 分类 |
| description | TEXT | 描述 |
| prompt_template | TEXT | 提示词模板 |
| style | VARCHAR(50) | 风格 |
| thumbnail_url | TEXT | 缩略图 |
| is_builtin | BOOLEAN | 是否内置 |
| usage_count | INT | 使用次数 |
| created_at | TIMESTAMP | 创建时间 |
| updated_at | TIMESTAMP | 更新时间 |

---

## 四、API设计

### 4.1 产品管理

```
POST   /api/products          # 包含图片上传,multipart/form-data
GET    /api/products?page=1&pageSize=20&category=xxx
GET    /api/products/{id}
PUT    /api/products/{id}
DELETE /api/products/{id}
POST   /api/products/{id}/images   # 上传图片,支持标记参考图
DELETE /api/products/{id}/images/{imageId}
```

### 4.2 生成任务

```
POST   /api/generation-tasks
POST   /api/generation-tasks/batch    # 多产品批量任务
GET    /api/generation-tasks?page=1&status=pending
GET    /api/generation-tasks/{id}
POST   /api/generation-tasks/{id}/cancel
```

**创建任务请求示例:**

```json
{
  "productId": "uuid-here",
  "imageCount": 3,
  "textVariantsCount": 5,
  "textTemplateIds": ["uuid-pain_point", "uuid-value"],
  "imageTemplateIds": ["uuid-kitchen", "uuid-living-room"],
  "useReferenceImage": true,
  "style": "realistic"
}
```

**校验规则:**
- `imageTemplateIds` 至少1项,为空时自动填充"通用场景"模板
- `useReferenceImage=true` 时,检查产品是否有 `type=reference` 图片,无则降级为文生图并返回提示

### 4.3 生成内容

```
GET    /api/generated-contents?taskId=xxx&templateCategory=kitchen&status=success&page=1
GET    /api/generated-images?taskId=xxx&templateId=xxx
GET    /api/generated-texts?taskId=xxx&templateId=xxx
POST   /api/generated-contents/{id}/select    # 选择图片和文案
POST   /api/generated-contents/batch/export   # 批量导出
```

**选择内容请求:**

```json
{
  "selectedImageId": "img-uuid",
  "selectedTextId": "text-uuid"
}
```

### 4.4 AI服务配置

```
GET    /api/ai-providers
PUT    /api/ai-providers/{type}/active
POST   /api/ai-providers/test
PUT    /api/ai-providers/{name}/config
```

**测试API连接:**

```json
{
  "providerName": "tongyi_wanxiang",
  "apiKey": "sk-xxx"
}
```

### 4.5 模板管理

```
GET    /api/templates?type=pain_point
POST   /api/templates
PUT    /api/templates/{id}
DELETE /api/templates/{id}
```

**图片模板 API:**

```
GET    /api/image-templates?category=kitchen
POST   /api/image-templates
PUT    /api/image-templates/{id}
DELETE /api/image-templates/{id}
POST   /api/image-templates/{id}/preview  # 低分辨率预览
```

**创建图片模板:**

```json
{
  "name": "厨房台面场景",
  "category": "kitchen",
  "description": "产品在整洁的现代厨房台面上",
  "promptTemplate": "一个{product_name}放在整洁的现代厨房台面上,自然光摄影风格,简约构图,高细节产品特写,生活化场景,4k画质,真实感",
  "style": "realistic"
}
```

### 4.6 统计分析

```
GET    /api/statistics/cost?startDate=2026-05-01&endDate=2026-05-29
GET    /api/statistics/generation?period=month
GET    /api/statistics/provider-usage
GET    /api/statistics/budget-status      # 今日预算剩余及预估可用次数
```

### 4.7 身份认证

```
POST   /api/auth/login
POST   /api/auth/register
GET    /api/auth/me
```

---

## 五、核心业务流程

### 5.1 批量生成流程 (优化后,支持并行子任务)

```mermaid
sequenceDiagram
    participant User as 用户
    participant Frontend as 前端
    participant API as API
    participant Budget as 预算预留服务(Redis)
    participant Hangfire as Hangfire
    participant Master as 主Job
    participant Worker as 子Job Worker
    participant AI as AI服务
    participant DB as PostgreSQL
    participant Storage as Supabase Storage

    User->>Frontend: 上传产品图+选择模板
    Frontend->>API: POST /api/products(含图片)
    API->>Storage: 上传产品图片
    Storage-->>API: URL
    API->>DB: 保存产品及product_images记录

    User->>Frontend: 配置参数并提交
    Frontend->>API: POST /api/generation-tasks
    API->>API: 校验参考图可用性,必要时降级
    API->>API: 预估成本
    API->>Budget: 尝试预留预算
    alt 预留成功
        Budget-->>API: ok
        API->>DB: 创建任务+task_image_templates
        API->>Hangfire: 入队主Job
        API-->>Frontend: taskId + 预估成本
    else 预留失败
        API-->>Frontend: 429 预算不足
    end

    Frontend->>API: 订阅SignalR
    API-->>Frontend: 连接

    Hangfire->>Master: 触发主Job
    Master->>DB: 更新状态(processing)
    Master->>API: 推送进度 (0%)

    loop 为每个图片模板及文案模板创建子Job
        Master->>Hangfire: 入队子Job(幂等键=taskId+templateId+type)
    end

    par 图片子Job并行执行(并发限制3)
        Hangfire->>Worker: 执行图片子Job
        Worker->>DB: 检查幂等键,有结果则跳过
        alt 图生图模式
            Worker->>Storage: 获取参考图URL
            Worker->>AI: 调用图生图API(携带CancellationToken)
        else 文生图
            Worker->>AI: 调用文生图API
        end
        AI-->>Worker: 返回图片
        Worker->>Storage: 上传图片
        Storage-->>Worker: 存储URL
        Worker->>DB: 写入generated_images(含cost)
        Worker->>API: 推送进度(当前步骤/总步骤)
    end
    par 文案子Job并行执行
        Worker->>AI: 生成文案
        AI-->>Worker: 返回
        Worker->>DB: 写入generated_texts
        Worker->>API: 推送进度
    end

    所有子Job完成后,主Job监听完成状态
    Master->>DB: 汇总actual_cost,更新任务completed
    Master->>Budget: 释放未消费预留额度
    Master->>API: 推送完成信号

    Frontend->>API: GET /api/generated-images + texts
    API-->>Frontend: 按模板分组数据
    Frontend->>User: 展示结果
```

**关键说明:**
- **参考图生成:** 如果用户上传了产品参考图且`useReferenceImages=true`,AI会基于参考图进行图生图(image-to-image),保持产品外观一致
- **模板化生成:** 用户可选择多个图片模板(如同时选择厨房+客厅+卧室),系统会为每个模板生成对应的场景图
- **结果组织:** 生成的内容按模板分组展示,方便用户对比不同场景效果

### 5.2 预算预留与释放

- 创建任务时,根据 `imageCount × 模板数 × 图片单价` + `textVariantsCount × 模板数 × 文案单价` 计算预估成本,调用 `BudgetReservationService.TryReserve` 在 Redis 中原子扣减。
- 任务完成/取消/失败时,计算实际费用,释放 `预留 - 实际` 的差额。
- 每日零时重置预算计数。

### 5.3 任务取消

- 前端调用 `POST /api/generation-tasks/{id}/cancel`
- API 调用 Hangfire 的 `BackgroundJob.Delete` 删除主 Job,并遍历未完成的子 Job 进行删除。
- 已开始的子 Job 通过 `CancellationToken` 终止 HTTP 请求,捕获异常并记录状态为 `cancelled`。
- 预算预留根据已完成部分和未完成部分进行结算。

---

## 六、AI服务插件化设计

### 6.1 接口定义

**IImageGenerator (图片生成接口)**

```csharp
public interface IImageGenerator
{
    string ProviderName { get; }
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default);
    Task<bool> ValidateConfigAsync(ProviderConfig config, CancellationToken ct = default);
}

public record ImageGenerationRequest(
    string Prompt,
    string Style = "realistic",
    string Size = "1024x1024",
    int BatchSize = 1,
    string? ReferenceImageUrl = null  // 可选的参考图URL(图生图模式)
);

public record ImageGenerationResult(
    bool Success,
    List<string>? ImageUrls = null,
    string? ErrorMessage = null,
    decimal Cost = 0
);
```

**ITextGenerator (文案生成接口)**

```csharp
public interface ITextGenerator
{
    string ProviderName { get; }
    Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default);
    Task<bool> ValidateConfigAsync(ProviderConfig config, CancellationToken ct = default);
}

public record TextGenerationRequest(
    Dictionary<string, string> ProductInfo,
    string TemplateType,
    string Tone = "friendly",
    int MaxLength = 300
);

public record TextGenerationResult(
    bool Success,
    List<string>? Texts = null,
    string? ErrorMessage = null,
    decimal Cost = 0
);
```

**ICostCalculator (成本计算器)**

```csharp
public interface ICostCalculator
{
    decimal CalculateImageCost(string providerName, string model, string size, bool useReference);
    decimal CalculateTextCost(string providerName, string model, int tokenCount);
    (decimal imageCost, decimal textCost) EstimateTaskCost(GenerationTask task, List<ImageTemplate> imgTemplates, List<TextTemplate> txtTemplates);
}
```

**接口说明:**
- `ReferenceImageUrl`: 如果提供,AI将使用图生图(image-to-image)模式,基于参考图生成保持产品外观一致的场景图
- 如果不提供,则使用纯文生图(text-to-image)模式
- `CancellationToken`: 支持任务取消时中断API调用

### 6.2 插件实现示例

**通义万相图片生成器**

```csharp
public class TongyiWanxiangGenerator : IImageGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;
    
    public string ProviderName => "通义万相";
    
    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                model = _model,
                input = new { 
                    prompt = request.Prompt,
                    reference_image_url = request.ReferenceImageUrl  // 图生图模式
                },
                parameters = new { 
                    n = request.BatchSize,
                    size = request.Size
                }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            var response = await _httpClient.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis",
                content,
                ct  // 传递CancellationToken
            );
            
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TongyiResponse>(resultJson);
                
                var urls = result?.Output?.Results?.Select(r => r.Url).ToList() ?? new List<string>();
                var cost = CalculateCost(request.BatchSize, request.ReferenceImageUrl != null);
                
                return new ImageGenerationResult(
                    Success: true,
                    ImageUrls: urls,
                    Cost: cost
                );
            }
            else
            {
                return new ImageGenerationResult(
                    Success: false,
                    ErrorMessage: $"API Error: {response.StatusCode}"
                );
            }
        }
        catch (OperationCanceledException)
        {
            return new ImageGenerationResult(
                Success: false,
                ErrorMessage: "Task cancelled"
            );
        }
        catch (Exception ex)
        {
            return new ImageGenerationResult(
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }
    
    public async Task<bool> ValidateConfigAsync(ProviderConfig config, CancellationToken ct = default)
    {
        // 验证API密钥
        var testRequest = new ImageGenerationRequest(
            Prompt: "test",
            BatchSize: 1
        );
        
        var result = await GenerateAsync(testRequest, ct);
        return result.Success || !result.ErrorMessage?.Contains("authentication") == true;
    }
    
    private decimal CalculateCost(int batchSize, bool useReference)
    {
        var basePrice = _model switch
        {
            "wan2.1-t2i-turbo" => 0.18m,
            "wan2.1-t2i-plus" => 0.37m,
            _ => 0.18m
        };
        
        // 图生图模式可能价格更高
        return basePrice * batchSize * (useReference ? 1.2m : 1.0m);
    }
}
```

**通义千问文案生成器**

```csharp
public class TongyiQianwenGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly Dictionary<string, string> _templates;
    
    public string ProviderName => "通义千问";
    
    public async Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildPrompt(request);
            
            var payload = new
            {
                model = _model,
                input = new { messages = new[] {
                    new { role = "user", content = prompt }
                }},
                parameters = new { temperature = 0.8 }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            var response = await _httpClient.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                content,
                ct
            );
            
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<QwenResponse>(resultJson);
                
                var texts = ParseTexts(result?.Output?.Text ?? "");
                var cost = CalculateCost(prompt.Length + (result?.Output?.Text?.Length ?? 0));
                
                return new TextGenerationResult(
                    Success: true,
                    Texts: texts,
                    Cost: cost
                );
            }
            else
            {
                return new TextGenerationResult(
                    Success: false,
                    ErrorMessage: $"API Error: {response.StatusCode}"
                );
            }
        }
        catch (OperationCanceledException)
        {
            return new TextGenerationResult(
                Success: false,
                ErrorMessage: "Task cancelled"
            );
        }
        catch (Exception ex)
        {
            return new TextGenerationResult(
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }
    
    public async Task<bool> ValidateConfigAsync(ProviderConfig config, CancellationToken ct = default)
    {
        var testRequest = new TextGenerationRequest(
            ProductInfo: new Dictionary<string, string> { ["name"] = "测试产品" },
            TemplateType: "pain_point"
        );
        
        var result = await GenerateAsync(testRequest, ct);
        return result.Success;
    }
    
    private string BuildPrompt(TextGenerationRequest request)
    {
        var template = _templates[request.TemplateType];
        var prompt = template;
        
        foreach (var kvp in request.ProductInfo)
        {
            prompt = prompt.Replace($"{{{kvp.Key}}}", kvp.Value);
        }
        
        return prompt;
    }
    
    private List<string> ParseTexts(string text)
    {
        return text.Split("---")
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
    }
    
    private decimal CalculateCost(int tokenCount)
    {
        return tokenCount * 0.000002m;
    }
}
```

### 6.3 弹性调用策略

在 AI 客户端中使用 Polly 策略:

- **重试策略:** HTTP 5xx / 超时 重试 3 次(指数退避)
- **限流策略:** 收到 429 后读取 `Retry-After` 头延迟执行
- **熔断策略:** 连续失败 5 次后暂停 30 秒
- **超时策略:** 单次请求 120 秒超时

```csharp
// Program.cs - Polly配置
builder.Services.AddHttpClient<IImageGenerator, TongyiWanxiangGenerator>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(120));
}
```

### 6.4 依赖注入配置

```csharp
// AIFactory.cs
public static class AIFactory
{
    public static IServiceCollection AddAIService(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册图片生成器
        services.AddSingleton<IImageGenerator>(sp =>
        {
            var activeProvider = configuration["AIProviders:ImageGeneration:ActiveProvider"];
            
            return activeProvider switch
            {
                "tongyi_wanxiang" => sp.GetRequiredService<TongyiWanxiangGenerator>(),
                "wenxin_yige" => sp.GetRequiredService<WenxinYigeGenerator>(),
                _ => throw new InvalidOperationException($"Unknown image provider: {activeProvider}")
            };
        });
        
        // 注册文案生成器
        services.AddSingleton<ITextGenerator>(sp =>
        {
            var activeProvider = configuration["AIProviders:TextGeneration:ActiveProvider"];
            
            return activeProvider switch
            {
                "tongyi_qianwen" => sp.GetRequiredService<TongyiQianwenGenerator>(),
                "wenxin_yiyan" => sp.GetRequiredService<WenxinYiyanGenerator>(),
                _ => throw new InvalidOperationException($"Unknown text provider: {activeProvider}")
            };
        });
        
        // 注册具体实现
        services.AddSingleton<TongyiWanxiangGenerator>();
        services.AddSingleton<WenxinYigeGenerator>();
        services.AddSingleton<TongyiQianwenGenerator>();
        services.AddSingleton<WenxinYiyanGenerator>();
        
        // 注册成本计算器
        services.AddSingleton<ICostCalculator, DefaultCostCalculator>();
        
        return services;
    }
}
```

### 6.5 配置文件

```json
{
  "AIProviders": {
    "ImageGeneration": {
      "ActiveProvider": "tongyi_wanxiang",
      
      "TongyiWanxiang": {
        "ApiKey": "${TONGYI_API_KEY}",
        "Model": "wan2.1-t2i-turbo"
      },
      
      "WenxinYige": {
        "ApiKey": "${WENXIN_API_KEY}",
        "SecretKey": "${WENXIN_SECRET_KEY}",
        "Model": "ernie-vilg-v2"
      }
    },
    
    "TextGeneration": {
      "ActiveProvider": "tongyi_qianwen",
      
      "TongyiQianwen": {
        "ApiKey": "${TONGYI_API_KEY}",
        "Model": "qwen-turbo"
      },
      
      "WenxinYiyan": {
        "ApiKey": "${WENXIN_API_KEY}",
        "SecretKey": "${WENXIN_SECRET_KEY}",
        "Model": "ernie-bot-4"
      }
    }
  },
  
  "GenerationStrategy": {
    "Image": {
      "RetryTimes": 3,
      "TimeoutSeconds": 120,
      "FallbackProvider": "wenxin_yige",
      "MaxConcurrency": 3
    },
    "Text": {
      "RetryTimes": 2,
      "VariantsCount": 5
    }
  },
  
  "CostControl": {
    "DailyBudget": 200.0,
    "AlertThreshold": 0.8,
    "AutoPause": true
  }
}
```

---

## 七、前端架构

### 7.1 页面结构

```
src/
├── components/
│   ├── Layout/
│   │   ├── MainLayout.tsx          # 主布局(侧边栏+顶栏)
│   │   └── Sidebar.tsx
│   ├── Product/
│   │   ├── ProductList.tsx         # 产品列表
│   │   ├── ProductForm.tsx         # 产品表单(含图片上传)
│   │   ├── ImageUploader.tsx       # 图片上传组件(支持拖拽/标记参考图)
│   │   └── ProductCard.tsx
│   ├── Generation/
│   │   ├── TaskConfig.tsx          # 任务配置(模板选择+预览)
│   │   ├── TemplateSelector.tsx    # 模板选择器(带预览按钮)
│   │   ├── TaskList.tsx            # 任务列表
│   │   ├── TaskProgress.tsx        # 进度显示(SignalR)
│   │   └── ContentPreview.tsx      # 内容预览(按模板分组)
│   ├── AISettings/
│   │   ├── ProviderConfig.tsx      # 提供商配置
│   │   └── ConnectionTest.tsx      # 连接测试
│   └── Dashboard/
│       ├── CostChart.tsx           # 成本图表
│       ├── BudgetStatus.tsx        # 预算状态
│       └── GenerationStats.tsx     # 生成统计
│
├── pages/
│   ├── Dashboard.tsx               # 仪表盘
│   ├── Products.tsx                # 产品管理
│   ├── Generate.tsx                # 内容生成
│   ├── Contents.tsx                # 内容库
│   ├── Settings.tsx                # 设置
│   └── Statistics.tsx              # 统计
│
├── services/
│   ├── api.ts                      # Axios配置
│   ├── productService.ts
│   ├── generationService.ts
│   └── signalr.ts                  # SignalR连接(自动重连)
│
├── hooks/
│   ├── useGenerationTask.ts        # 任务Hook
│   ├── useCostMonitor.ts           # 成本监控Hook
│   └── useSignalR.ts               # SignalR Hook(自动重连)
│
├── store/
│   └── index.ts                    # Zustand状态
│
└── types/
    └── index.ts                    # TypeScript类型
```

### 7.2 关键组件示例

**SignalR连接 (自动重连)**

```typescript
import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';

export const useSignalR = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  
  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/generation', {
        transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();
    
    conn.onreconnected(async (connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      // 重连后自动重新订阅任务
      await conn.invoke('ResubscribeToTasks');
    });
    
    conn.start().catch(err => console.error('SignalR connection failed:', err));
    
    setConnection(conn);
    
    return () => {
      conn.stop();
    };
  }, []);
  
  return { connection };
};
```

**任务进度组件 (使用SignalR)**

```tsx
import { useEffect, useState } from 'react';
import { ProgressBar, Text } from '@mantine/core';
import { useSignalR } from '../hooks/useSignalR';

interface TaskProgressProps {
  taskId: string;
  onComplete?: () => void;
}

export const TaskProgress: React.FC<TaskProgressProps> = ({ taskId, onComplete }) => {
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState('pending');
  const { connection } = useSignalR();
  
  useEffect(() => {
    if (!connection) return;
    
    connection.on('TaskProgressUpdated', (data) => {
      if (data.taskId === taskId) {
        setProgress(data.progress);
        setStatus(data.status);
        
        if (data.status === 'completed') {
          onComplete?.();
        }
      }
    });
    
    connection.invoke('SubscribeToTask', taskId);
    
    return () => {
      connection.invoke('UnsubscribeFromTask', taskId);
    };
  }, [connection, taskId]);
  
  return (
    <div>
      <ProgressBar value={progress} color="pink.6" size="lg" />
      <Text mt="sm">{status}: {progress}%</Text>
    </div>
  );
};
```

**内容预览组件 (按模板分组)**

```tsx
import { Image, Card, Button, Group, Tabs } from '@mantine/core';
import { useState } from 'react';

interface ContentPreviewProps {
  imagesByTemplate: Record<string, GeneratedImage[]>;
  textsByTemplate: Record<string, GeneratedText[]>;
  onSelect: (imageId: string, textId: string) => void;
}

export const ContentPreview: React.FC<ContentPreviewProps> = ({
  imagesByTemplate,
  textsByTemplate,
  onSelect
}) => {
  const [activeTemplate, setActiveTemplate] = useState<string | null>(null);
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [selectedText, setSelectedText] = useState<string | null>(null);
  
  const templates = Object.keys(imagesByTemplate);
  
  if (templates.length === 0) {
    return <div>暂无生成内容</div>;
  }
  
  return (
    <Tabs value={activeTemplate} onChange={setActiveTemplate}>
      <Tabs.List>
        {templates.map(templateId => (
          <Tabs.Tab key={templateId} value={templateId}>
            {templateId}
          </Tabs.Tab>
        ))}
      </Tabs.List>
      
      {templates.map(templateId => (
        <Tabs.Panel key={templateId} value={templateId}>
          <Group position="apart" mt="md">
            {/* 图片列表 */}
            <div style={{ flex: 1 }}>
              {imagesByTemplate[templateId]?.map(img => (
                <Card
                  key={img.id}
                  shadow="sm"
                  padding="xs"
                  mb="sm"
                  style={{
                    border: selectedImage === img.id ? '2px solid #e91e63' : 'none'
                  }}
                  onClick={() => setSelectedImage(img.id)}
                >
                  <Card.Section>
                    <Image src={img.imageUrl} height={200} fit="cover" />
                  </Card.Section>
                </Card>
              ))}
            </div>
            
            {/* 文案列表 */}
            <div style={{ flex: 1, marginLeft: 16 }}>
              {textsByTemplate[templateId]?.map(text => (
                <Card
                  key={text.id}
                  shadow="sm"
                  padding="sm"
                  mb="sm"
                  style={{
                    border: selectedText === text.id ? '2px solid #e91e63' : 'none'
                  }}
                  onClick={() => setSelectedText(text.id)}
                >
                  <Text size="sm">{text.content}</Text>
                </Card>
              ))}
            </div>
          </Group>
          
          <Group mt="md" position="right">
            <Button
              color="pink.6"
              disabled={!selectedImage || !selectedText}
              onClick={() => selectedImage && selectedText && onSelect(selectedImage, selectedText)}
            >
              选择此组合
            </Button>
          </Group>
        </Tabs.Panel>
      ))}
    </Tabs>
  );
};
```

---

## 八、后台任务设计

### 8.1 Hangfire配置

```csharp
// Program.cs
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// 配置Hangfire
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
    config.SetWorkerCount(5); // 并发Worker数量
});

builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() } // 需要认证
});
```

### 8.2 主/子任务模式

```csharp
// Jobs/ContentGenerationJobs.cs
public class ContentGenerationJobs
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly ILogger<ContentGenerationJobs> _logger;
    
    public ContentGenerationJobs(
        ApplicationDbContext db,
        IHubContext<GenerationHub> hubContext,
        ILogger<ContentGenerationJobs> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }
    
    [Queue("generation")]
    public async Task ProcessMasterTask(Guid taskId, CancellationToken ct)
    {
        var task = await _db.GenerationTasks.FindAsync(taskId);
        if (task == null) return;
        
        var imgTemplates = await _db.TaskImageTemplates
            .Where(t => t.TaskId == taskId)
            .ToListAsync(ct);
        
        var totalSteps = imgTemplates.Count * task.ImageCount + 
                        task.TextTemplateIds.Count * task.TextVariantsCount;
        var completedSteps = 0;
        
        // 更新状态
        task.Status = "processing";
        task.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        
        await UpdateProgress(taskId, 0, "开始生成...");
        
        // 创建图片子Job
        var imageJobs = new List<string>();
        foreach (var imgTpl in imgTemplates)
        {
            for (int i = 0; i < task.ImageCount; i++)
            {
                var jobId = $"img_{taskId}_{imgTpl.ImageTemplateId}_{i}";
                BackgroundJob.Enqueue<ISubJobExecutor>(
                    x => x.GenerateImage(jobId, taskId, imgTpl.ImageTemplateId, ct)
                );
                imageJobs.Add(jobId);
            }
        }
        
        // 创建文案子Job
        foreach (var txtTplId in task.TextTemplateIds)
        {
            for (int i = 0; i < task.TextVariantsCount; i++)
            {
                var jobId = $"txt_{taskId}_{txtTplId}_{i}";
                BackgroundJob.Enqueue<ISubJobExecutor>(
                    x => x.GenerateText(jobId, taskId, txtTplId, ct)
                );
            }
        }
        
        // 监听子Job完成状态,更新总进度
        while (!ct.IsCancellationRequested)
        {
            var completedImages = await _db.GeneratedImages
                .CountAsync(i => i.TaskId == taskId && i.Status == "success", ct);
            
            var completedTexts = await _db.GeneratedTexts
                .CountAsync(t => t.TaskId == taskId && t.Status == "success", ct);
            
            completedSteps = completedImages + completedTexts;
            var progress = (int)((decimal)completedSteps / totalSteps * 100);
            
            await UpdateProgress(taskId, progress, $"已完成 {completedSteps}/{totalSteps}");
            
            if (completedSteps >= totalSteps)
            {
                break;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
        
        // 汇总实际成本
        var actualCost = await _db.GeneratedImages
            .Where(i => i.TaskId == taskId)
            .SumAsync(i => i.Cost, ct);
        
        actualCost += await _db.GeneratedTexts
            .Where(t => t.TaskId == taskId)
            .SumAsync(t => t.Cost, ct);
        
        task.ActualCost = actualCost;
        task.Status = "completed";
        task.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        
        await UpdateProgress(taskId, 100, "完成!");
        
        // 释放未消费的预算预留
        var budgetService = _db.GetService<BudgetReservationService>();
        await budgetService.Release(task.UserId, task.EstimatedCost - actualCost);
    }
}

public interface ISubJobExecutor
{
    Task GenerateImage(string jobId, Guid taskId, Guid templateId, CancellationToken ct);
    Task GenerateText(string jobId, Guid taskId, Guid templateId, CancellationToken ct);
}

public class SubJobExecutor : ISubJobExecutor
{
    private readonly ApplicationDbContext _db;
    private readonly IImageGenerator _imageGenerator;
    private readonly ITextGenerator _textGenerator;
    private readonly IHubContext<GenerationHub> _hubContext;
    
    public async Task GenerateImage(string jobId, Guid taskId, Guid templateId, CancellationToken ct)
    {
        // 幂等检查
        if (await _db.GeneratedImages.AnyAsync(i => i.IdempotencyKey == jobId, ct))
        {
            return;
        }
        
        var task = await _db.GenerationTasks.FindAsync(new object[] { taskId }, ct);
        var template = await _db.ImageTemplates.FindAsync(new object[] { templateId }, ct);
        
        try
        {
            // 构建prompt
            var prompt = BuildPrompt(template.PromptTemplate, task.Product);
            
            // 获取参考图(如果需要)
            string? referenceImageUrl = null;
            if (task.UseReferenceImage)
            {
                var refImage = await _db.ProductImages
                    .FirstOrDefaultAsync(p => p.ProductId == task.ProductId && p.Type == "reference", ct);
                referenceImageUrl = refImage?.Url;
            }
            
            // 调用AI生成
            var result = await _imageGenerator.GenerateAsync(new ImageGenerationRequest(
                Prompt: prompt,
                Style: template.Style,
                BatchSize: 1,
                ReferenceImageUrl: referenceImageUrl
            ), ct);
            
            if (result.Success && result.ImageUrls.Any())
            {
                // 保存图片
                var generatedImage = new GeneratedImage
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    ImageTemplateId = templateId,
                    ImageUrl = result.ImageUrls[0],
                    Cost = result.Cost,
                    Status = "success",
                    IdempotencyKey = jobId
                };
                
                _db.GeneratedImages.Add(generatedImage);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // 记录失败
                var failedImage = new GeneratedImage
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    ImageTemplateId = templateId,
                    Cost = 0,
                    Status = "failed",
                    ErrorMessage = result.ErrorMessage,
                    IdempotencyKey = jobId
                };
                
                _db.GeneratedImages.Add(failedImage);
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"Image job {jobId} cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Image job {jobId} failed");
            
            var failedImage = new GeneratedImage
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ImageTemplateId = templateId,
                Cost = 0,
                Status = "failed",
                ErrorMessage = ex.Message,
                IdempotencyKey = jobId
            };
            
            _db.GeneratedImages.Add(failedImage);
            await _db.SaveChangesAsync(ct);
        }
    }
    
    public async Task GenerateText(string jobId, Guid taskId, Guid templateId, CancellationToken ct)
    {
        // 类似图片生成的实现
        // ...
    }
    
    private string BuildPrompt(string template, Product product)
    {
        var prompt = template;
        prompt = prompt.Replace("{product_name}", product.Name);
        prompt = prompt.Replace("{price}", product.Price.ToString());
        prompt = prompt.Replace("{selling_points}", string.Join(",", product.SellingPoints));
        return prompt;
    }
}
```

### 8.3 任务取消

```csharp
// Controllers/GenerationTasksController.cs
[HttpPost("{id}/cancel")]
public async Task<IActionResult> CancelTask(Guid id)
{
    var task = await _db.GenerationTasks.FindAsync(id);
    if (task == null)
    {
        return NotFound();
    }
    
    if (task.Status == "completed" || task.Status == "failed")
    {
        return BadRequest("Task already finished");
    }
    
    // 删除主Job
    BackgroundJob.Delete($"master_{id}");
    
    // 删除所有未完成的子Job
    var unfinishedJobs = await _db.BackgroundJobs
        .Where(j => j.TaskId == id && j.Status != "completed")
        .ToListAsync();
    
    foreach (var job in unfinishedJobs)
    {
        BackgroundJob.Delete(job.JobId);
    }
    
    // 更新任务状态
    task.Status = "cancelled";
    task.CompletedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    
    // 结算预算
    var actualCost = await _db.GeneratedImages
        .Where(i => i.TaskId == id && i.Status == "success")
        .SumAsync(i => i.Cost);
    
    actualCost += await _db.GeneratedTexts
        .Where(t => t.TaskId == id && t.Status == "success")
        .SumAsync(t => t.Cost);
    
    var budgetService = _serviceProvider.GetRequiredService<BudgetReservationService>();
    await budgetService.Release(task.UserId, task.EstimatedCost - actualCost);
    
    return Ok();
}
```

---

## 九、成本控制设计

### 9.1 预算预留服务

```csharp
// Services/BudgetReservationService.cs
public class BudgetReservationService
{
    private readonly IDatabase _redis;
    private readonly decimal _dailyBudget;
    
    public BudgetReservationService(IConfiguration configuration, IConnectionMultiplexer redis)
    {
        _dailyBudget = configuration.GetValue<decimal>("CostControl:DailyBudget");
        _redis = redis.GetDatabase();
    }
    
    public async Task<bool> TryReserve(Guid userId, Guid taskId, decimal amount)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        
        // 原子操作: 获取当前已用预算
        var used = (decimal)await _redis.StringGetAsync(key);
        
        if (used + amount > _dailyBudget)
        {
            return false;
        }
        
        // 原子增加
        var newValue = await _redis.StringIncrementAsync(key, (double)amount);
        
        if (newValue > (double)_dailyBudget)
        {
            // 回滚
            await _redis.StringDecrementAsync(key, (double)amount);
            return false;
        }
        
        // 记录预留详情
        await _redis.HashSetAsync($"budget:reservations:{userId}", taskId.ToString(), amount.ToString());
        
        return true;
    }
    
    public async Task Release(Guid userId, decimal amount)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        await _redis.StringDecrementAsync(key, (double)amount);
    }
    
    public async Task<decimal> GetRemainingBudget(Guid userId)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        var used = (decimal)await _redis.StringGetAsync(key);
        return _dailyBudget - used;
    }
    
    public async Task<int> GetEstimatedRemainingTasks(Guid userId)
    {
        var remaining = await GetRemainingBudget(userId);
        var avgCostPerTask = 2.5m; // 平均每套成本
        return (int)(remaining / avgCostPerTask);
    }
}
```

### 9.2 预算中间件

```csharp
// Middleware/BudgetGuardMiddleware.cs
public class BudgetGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BudgetReservationService _budgetService;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // 仅对生成相关的API进行预算检查
        if (context.Request.Path.StartsWithSegments("/api/generation") && 
            context.Request.Method == "POST")
        {
            var userId = context.User.GetUserId();
            var remaining = await _budgetService.GetRemainingBudget(userId);
            
            if (remaining <= 0)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Daily budget exceeded",
                    message = "今日预算已用完,请明天再试或联系管理员增加预算"
                });
                return;
            }
        }
        
        await _next(context);
    }
}

// Program.cs
app.UseMiddleware<BudgetGuardMiddleware>();
```

### 9.3 成本记录

- 各子 Job 完成时,在 `generated_images.cost` / `generated_texts.cost` 记录实际消耗。
- 每日汇总可查询物化视图 `cost_records_summary` 或直接聚合生成明细表。

---

## 十、安全设计

### 10.1 认证授权

- **JWT Token认证:** 用户登录后获取JWT,后续请求携带Token
- **角色权限:** Admin(管理员) / Operator(操作员)
- **用户数据隔离:** 所有业务接口默认过滤当前登录用户的数据,Admin 可跨用户查询
- **API密钥加密存储:** AI服务的API密钥使用 .NET Data Protection API 加密后存入数据库

### 10.2 速率限制

```csharp
// 使用AspNetCore.RateLimit
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("generation", opt =>
    {
        opt.PermitLimit = 100; // 每小时最多100次生成请求
        opt.Window = TimeSpan.FromHours(1);
    });
});

// 在Controller上应用
[EnableRateLimiting("generation")]
[HttpPost]
public async Task<IActionResult> CreateTask(...)
```

### 10.3 数据安全

- **敏感环境变量:** Docker secrets 替代 Compose 明文环境变量
- **数据备份:** 每日 pg_dump 并上传 Supabase Storage,保留 30 天
- **防止SQL注入:** EF Core参数化查询
- **防止XSS:** 前端输出转义

### 10.4 Docker Secrets配置

```yaml
# docker-compose.yml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: douyin_content
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    secrets:
      - db_password
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  api:
    build: ./api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DataProtection__KeyPath=/run/secrets/dp_key
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started
    secrets:
      - dp_key
      - ai_keys

  frontend:
    build: ./frontend
    ports:
      - "3000:80"
    depends_on:
      - api

volumes:
  postgres_data:

secrets:
  db_password:
    file: ./secrets/db_password.txt
  dp_key:
    file: ./secrets/dp_key.txt
  ai_keys:
    file: ./secrets/ai_keys.json
```

---

## 十一、部署架构

### 11.1 Docker Compose完整配置

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: douyin_content
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./backups:/backups
    secrets:
      - db_password
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data

  api:
    build: 
      context: ./api
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=douyin_content;Username=postgres;PasswordFile=/run/secrets/db_password
      - DataProtection__KeyPath=/run/secrets/dp_key
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started
    secrets:
      - dp_key
      - ai_keys
    volumes:
      - logs:/app/logs

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      - api

  backup:
    image: postgres:16
    environment:
      PGPASSWORD_FILE: /run/secrets/db_password
    volumes:
      - ./scripts/backup.sh:/backup.sh
      - ./backups:/backups
    secrets:
      - db_password
    command: /bin/bash /backup.sh
    depends_on:
      - postgres

volumes:
  postgres_data:
  redis_data:
  logs:

secrets:
  db_password:
    file: ./secrets/db_password.txt
  dp_key:
    file: ./secrets/dp_key.txt
  ai_keys:
    file: ./secrets/ai_keys.json
```

### 11.2 环境变量

```bash
# .env
DB_PASSWORD=your_secure_password
TONGYI_API_KEY=sk-your-api-key
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_KEY=your-supabase-key
JWT_SECRET=your_jwt_secret_key
DATA_PROTECTION_KEY=your_dp_key
```

### 11.3 备份脚本

```bash
#!/bin/bash
# scripts/backup.sh

BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/douyin_content_$DATE.sql.gz"

# 执行备份
pg_dump -U postgres -h postgres douyin_content | gzip > $BACKUP_FILE

# 删除30天前的备份
find $BACKUP_DIR -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: $BACKUP_FILE"
```

---

## 十二、测试策略

### 12.1 单元测试

- AI服务接口mock测试
- 业务逻辑单元测试
- 成本计算逻辑测试
- 预算预留并发测试

### 12.2 集成测试

- API端点集成测试
- 数据库操作测试
- Hangfire任务测试
- **任务取消集成测试:** 验证取消任务能真正终止 AI HTTP 请求并正确释放预算
- **数据隔离测试:** 用户 A 不能访问用户 B 的产品和任务

### 12.3 端到端测试

- Playwright自动化测试
- 关键用户流程测试(创建产品→生成内容→查看结果)

---

## 十三、性能优化

### 13.1 后端优化

- **异步处理:** 所有I/O操作使用async/await
- **数据库索引:** 
  - `generated_images(task_id, image_template_id)`
  - `generated_texts(task_id, text_template_id)`
  - `products(user_id, category)`
- **缓存:** 文案模板、图片模板、产品列表使用Redis缓存
- **批量操作:** 批量插入生成结果
- **图片生成子Job并行度控制:** 全局并发信号量(如 3)防止 API 限流

### 13.2 前端优化

- **代码分割:** React.lazy按需加载组件
- **图片懒加载:** Intersection Observer实现
- **虚拟滚动:** 大量内容列表使用虚拟列表
- **请求合并:** 多个API请求合并

### 13.3 AI调用优化

- **并发控制:** 限制同时进行的AI调用数量
- **重试机制:** API失败自动重试(指数退避)
- **结果缓存:** 相同prompt的结果缓存复用

---

## 十四、MVP功能范围

### 第一版包含 (优化后):

✅ 用户认证与数据隔离  
✅ 产品管理(含多图上传与参考图标记)  
✅ 图片模板与文案模板管理  
✅ 按模板批量生成(支持图生图/文生图自动降级)  
✅ 并行子任务执行  
✅ SignalR 详细进度反馈  
✅ 预算预留与实时显示  
✅ 内容按模板分组预览、批量选择导出  
✅ AI 提供商可插拔切换  
✅ 成本明细到每次生成  
✅ 基础统计分析  

### 暂不包含(后续迭代):

❌ 自动发布到抖音(需要抖音开放平台权限)  
❌ A/B测试数据分析  
❌ 自定义训练模型  
❌ 移动端App  
❌ 智能抠图功能(第二版)  

---

## 十五、开发计划

### Week 1-2: 后端核心
- [ ] 数据库重构及迁移脚本
- [ ] 产品图片分离、用户表实现
- [ ] 生成内容拆表、AI 接口增强(CancellationToken、ValidateConfigAsync)
- [ ] ICostCalculator 实现
- [ ] 产品管理API
- [ ] 通义万相/通义千问插件实现

### Week 3: 任务调度与预算
- [ ] 主/子任务模式实现
- [ ] 预算预留服务(Redis)
- [ ] 任务取消完整逻辑
- [ ] Hangfire配置

### Week 4: 前端基础与生成
- [ ] React项目初始化
- [ ] Mantine UI集成
- [ ] 路由配置
- [ ] 前端图片上传组件、模板预览
- [ ] 内容预览页按模板分组重做
- [ ] SignalR 进度细化

### Week 5: 高级功能
- [ ] 权限体系与数据隔离集成
- [ ] AI提供商配置管理
- [ ] 成本统计仪表板
- [ ] 批量任务 API
- [ ] 数据统计看板

### Week 6: 测试优化
- [ ] 单元测试
- [ ] 集成测试(任务取消/预算并发/数据隔离)
- [ ] 性能优化
- [ ] 安全加固、备份脚本
- [ ] 部署配置
- [ ] 文档编写

---

## 十六、风险与应对

### 技术风险

| 风险 | 影响 | 应对措施 |
|------|------|----------|
| AI API不稳定 | 高 | 实现多提供商切换+Polly重试/熔断 |
| 成本超支 | 高 | Redis预算预留机制+实时告警 |
| 生成质量不达预期 | 中 | Prompt工程优化+人工筛选 |
| SignalR连接问题 | 低 | 自动重连+降级为Polling轮询 |
| AI 成本估算不准 | 中 | 预留一定缓冲(如1.2倍),任务完成后释放差额 |
| 并行子任务调度复杂 | 中 | 引入幂等键,子 Job 独立、可重试 |
| 用户大量任务时 Redis 预算计数竞争 | 低 | 使用 Redis 原子操作,必要时采用本地队列削峰 |

### 业务风险

| 风险 | 影响 | 应对措施 |
|------|------|----------|
| 抖音平台规则变化 | 高 | 持续关注平台政策 |
| 市场竞争加剧 | 中 | 快速迭代+差异化功能 |
| 用户需求变化 | 中 | 保持灵活性+快速响应 |

---

## 十七、成功标准

### 技术指标

- API响应时间 < 500ms (不含AI调用)
- 生成任务完成率 > 95%
- 系统可用性 > 99%
- 并发处理能力: 支持10个任务同时处理

### 业务指标

- 单次生成成本 < ¥5.00
- 内容采纳率 > 30%
- 用户满意度 > 4.0/5.0
- 日均生成量: 50-200套

---

## 附录

### A. 文案模板示例

**痛点型模板:**
```
你是一个抖音带货文案专家。请为{product_name}写一个痛点型文案。

要求:
1. 开头用情绪词抓住眼球(如"绝了""救命")
2. 描述用户痛点场景
3. 说明产品如何解决痛点
4. 突出价格优势
5. 结尾引导行动

产品信息:
- 名称: {product_name}
- 价格: {price}
- 卖点: {selling_points}

请生成3个不同版本的文案,用"---"分隔。
```

**性价比型模板:**
```
你是一个抖音带货文案专家。请为{product_name}写一个性价比型文案。

要求:
1. 强调价格优势
2. 对比同类产品
3. 说明质量不打折
4. 制造紧迫感

产品信息:
- 名称: {product_name}
- 价格: {price}
- 卖点: {selling_points}

请生成3个不同版本的文案,用"---"分隔。
```

### B. 图片模板示例

**内置图片模板(预置10+场景):**

| 模板名称 | 分类 | Prompt模板 |
|---------|------|-----------|
| 厨房台面场景 | kitchen | 一个{product_name}放在整洁的现代厨房台面上,自然光摄影风格,简约构图,高细节产品特写,生活化场景,4k画质,真实感 |
| 客厅展示场景 | living_room | 一个{product_name}在温馨的客厅环境中,柔和光线,现代简约风格,产品展示清晰,生活方式摄影,高质量,真实拍摄效果 |
| 卧室床头场景 | bedroom | 一个{product_name}放在整洁的卧室床头柜上,温暖晨光,舒适家居氛围,高细节,真实摄影风格 |
| 办公桌场景 | office | 一个{product_name}在现代办公桌上,简洁商务风格,自然光,专业产品摄影,高品质 |
| 浴室场景 | bathroom | 一个{product_name}在明亮的浴室环境中,清新干净,现代简约,产品特写,真实拍摄效果 |
| 户外自然场景 | outdoor | 一个{product_name}在户外自然环境中,阳光充足,清新自然风格,生活方式摄影,高质量 |
| 餐厅餐桌场景 | dining | 一个{product_name}在精致的餐厅餐桌上,温馨用餐氛围,暖色调光线,美食摄影风格 |
| 书房书架场景 | study | 一个{product_name}在木质书架上,文艺复古风格,柔和灯光,静物摄影,高质感 |
| 阳台休闲场景 | balcony | 一个{product_name}在阳光明媚的阳台上,轻松惬意,绿植点缀,生活气息,自然光摄影 |
| 玄关入口场景 | entrance | 一个{product_name}在现代家居玄关处,简约大气,入门第一印象,高格调摄影 |

**自定义模板创建示例:**

```json
{
  "name": "儿童房场景",
  "category": "kids_room",
  "description": "产品在温馨的儿童房中,适合母婴/玩具类产品",
  "promptTemplate": "一个{product_name}在色彩缤纷的儿童房中,温馨可爱风格,柔和光线,童趣装饰背景,亲子互动氛围,高质量摄影",
  "style": "warm"
}
```

### C. 参考图生成说明

**图生图(Image-to-Image)模式:**

当用户上传产品参考图并启用`useReferenceImages`时,AI会:

1. **保持产品外观一致** - 生成的图片中产品形状、颜色、细节与参考图高度相似
2. **替换背景场景** - 将产品放置到目标场景(厨房/客厅等)中
3. **调整光影效果** - 产品光照与场景光源匹配,增强真实感

**适用场景:**
- 已有产品实拍图,需要快速生成多场景宣传图
- 需要保持产品外观严格一致的品牌推广
- 电商产品详情页需要统一视觉风格

**注意事项:**
- 参考图质量影响生成效果,建议使用清晰、光线良好的产品图
- 某些AI提供商的图生图功能可能需要额外费用或更高配额
- 生成时间比纯文生图略长(约增加20-30%)

---

**文档结束**
