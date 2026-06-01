using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.AI.TextGenerators;

namespace DouyinContentGenerator.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.ImageTemplates.Any()) return;

        var templates = new List<ImageTemplate>
        {
            // === Product Photography (White Background) ===
            new() { Id = Guid.NewGuid(), Name = "白底产品特写", Category = "product_shot", IsBuiltin = true,
                Description = "纯白背景商业产品摄影", Style = "commercial",
                PromptTemplate = "professional product photography of a {product_name}, isolated on pure white background, studio lighting, 8k resolution, ultra detailed product texture, commercial photography, centered composition, clean and minimalist, no shadows on background, shot on DSLR Canon EOS 5D Mark IV, 100mm macro lens, f/8, product photography for ecommerce" },
            new() { Id = Guid.NewGuid(), Name = "厨房台面高级感", Category = "kitchen", IsBuiltin = true,
                Description = "阳光厨房台面产品展示", Style = "lifestyle",
                PromptTemplate = "a beautiful {product_name} on a modern marble kitchen countertop, morning sunlight streaming through window, soft natural lighting, bokeh background, fresh herbs and ingredients scattered artistically, luxury lifestyle photography, warm tones, shallow depth of field, 4k, editorial photography style, highly detailed" },
            new() { Id = Guid.NewGuid(), Name = "客厅氛围感", Category = "living_room", IsBuiltin = true,
                Description = "温馨客厅产品场景", Style = "lifestyle",
                PromptTemplate = "cozy living room setting featuring {product_name} on a wooden coffee table, warm golden hour light, cream and beige color palette, Scandinavian interior design style, steaming cup of coffee nearby, hygge atmosphere, lifestyle photography, soft natural light from large window, depth of field, high-end interior magazine quality" },
            new() { Id = Guid.NewGuid(), Name = "卧室清晨感", Category = "bedroom", IsBuiltin = true,
                Description = "晨光卧室产品展示", Style = "atmospheric",
                PromptTemplate = "{product_name} on a white wooden nightstand in a bright bedroom, soft morning light through sheer curtains, fluffy white bedding, trailing plant in background, warm and clean atmosphere, minimalist aesthetic, 4k, lifestyle product photography, serene mood, pastel color tones, bokeh effect" },
            new() { Id = Guid.NewGuid(), Name = "办公桌商务感", Category = "office", IsBuiltin = true,
                Description = "现代办公桌产品展示", Style = "professional",
                PromptTemplate = "{product_name} on a modern minimal desk setup, natural side lighting, clean workspace with macbook and notebook, gray and white tones, professional product photography, sharp focus on product, modern office aesthetic, 8k, commercial photography style, architectural digest quality" },
            new() { Id = Guid.NewGuid(), Name = "社交媒体广告风", Category = "social_media", IsBuiltin = true,
                Description = "适合抖音/小红书的社交电商风格", Style = "social_commerce",
                PromptTemplate = "{product_name} displayed with creative flat lay arrangement, surrounded by aesthetic matching props, vibrant and eye-catching colors, softbox studio lighting, trending social media product display style, optimized for mobile viewing, bright and clean, high contrast, visually striking, douyin xiaohongshu style product photography, 4k" },
            new() { Id = Guid.NewGuid(), Name = "户外自然光", Category = "outdoor", IsBuiltin = true,
                Description = "户外自然光产品拍摄", Style = "natural",
                PromptTemplate = "{product_name} on a picnic blanket in a sunlit park, dappled sunlight through trees, fresh green grass background, outdoor lifestyle photography, golden hour glow, natural environment, candid moment feel, soft bokeh nature background, high quality, travel lifestyle aesthetic, warm summer vibes" },
            new() { Id = Guid.NewGuid(), Name = "大理石高级感", Category = "luxury", IsBuiltin = true,
                Description = "大理石纹理背景高端产品", Style = "luxury",
                PromptTemplate = "luxury {product_name} on elegant white marble surface with subtle gold accents, dramatic side lighting creating depth and shadows, high-end commercial photography, premium brand aesthetic, dark moody background with rim light, product hero shot, perfume and jewelry style photography, ultra premium, sophisticated elegance, 8k" },
            new() { Id = Guid.NewGuid(), Name = "手拿/使用场景", Category = "usage", IsBuiltin = true,
                Description = "手持产品真实使用场景", Style = "authentic",
                PromptTemplate = "close-up shot of hands elegantly holding {product_name}, natural skin tones, cozy sweater sleeve, lifestyle action shot, authentic real-use moment, soft window light, shallow depth of field, warm skin tones, editorial style, relatable and aspirational, real person feeling, 4k" },
            new() { Id = Guid.NewGuid(), Name = "礼盒开箱风", Category = "unboxing", IsBuiltin = true,
                Description = "精致礼盒开箱产品展示", Style = "unboxing",
                PromptTemplate = "{product_name} inside an elegant unboxing setup, kraft paper and ribbon packaging, soft daylight, flat lay photography, gift-giving aesthetic, tissue paper texture visible, cozy home vibes, high quality product reveal, ecommerce unboxing experience, warm and inviting, 4k, top-down shot" },
        };

        db.ImageTemplates.AddRange(templates);

        var copywritingTemplates = new List<CopywritingTemplate>
        {
            new() { Id = Guid.NewGuid(), Name = "🎯 痛点暴力种草", TemplateType = "pain_point", IsBuiltin = true,
                Content = CopywritingTemplates.BuiltInTemplates["pain_point"],
                Variables = new[] { "product_name", "price", "selling_points" } },
            new() { Id = Guid.NewGuid(), Name = "💰 专业测评推荐", TemplateType = "value", IsBuiltin = true,
                Content = CopywritingTemplates.BuiltInTemplates["value"],
                Variables = new[] { "product_name", "price", "selling_points" } },
            new() { Id = Guid.NewGuid(), Name = "✨ 场景氛围种草", TemplateType = "scenario", IsBuiltin = true,
                Content = CopywritingTemplates.BuiltInTemplates["scenario"],
                Variables = new[] { "product_name", "price", "selling_points" } },
            new() { Id = Guid.NewGuid(), Name = "🔥 爆款话题营销", TemplateType = "trending", IsBuiltin = true,
                Content = CopywritingTemplates.BuiltInTemplates["trending"],
                Variables = new[] { "product_name", "price", "selling_points" } },
        };

        db.CopywritingTemplates.AddRange(copywritingTemplates);
        await db.SaveChangesAsync();
    }
}
