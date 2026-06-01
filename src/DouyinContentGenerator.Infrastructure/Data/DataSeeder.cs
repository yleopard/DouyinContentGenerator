using DouyinContentGenerator.Core.Models;

namespace DouyinContentGenerator.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.ImageTemplates.Any()) return;

        var templates = new List<ImageTemplate>
        {
            new() { Id = Guid.NewGuid(), Name = "厨房台面场景", Category = "kitchen", IsBuiltin = true,
                Description = "产品在整洁的现代厨房台面上", Style = "realistic",
                PromptTemplate = "一个{product_name}放在整洁的现代厨房台面上,自然光摄影风格,简约构图,高细节产品特写,生活化场景,4k画质,真实感" },
            new() { Id = Guid.NewGuid(), Name = "客厅展示场景", Category = "living_room", IsBuiltin = true,
                Description = "产品在温馨的客厅环境中", Style = "realistic",
                PromptTemplate = "一个{product_name}在温馨的客厅环境中,柔和光线,现代简约风格,产品展示清晰,生活方式摄影,高质量,真实拍摄效果" },
            new() { Id = Guid.NewGuid(), Name = "卧室床头场景", Category = "bedroom", IsBuiltin = true,
                Description = "产品放在整洁的卧室床头柜上", Style = "realistic",
                PromptTemplate = "一个{product_name}放在整洁的卧室床头柜上,温暖晨光,舒适家居氛围,高细节,真实摄影风格" },
            new() { Id = Guid.NewGuid(), Name = "办公桌场景", Category = "office", IsBuiltin = true,
                Description = "产品在现代办公桌上", Style = "realistic",
                PromptTemplate = "一个{product_name}在现代办公桌上,简洁商务风格,自然光,专业产品摄影,高品质" },
            new() { Id = Guid.NewGuid(), Name = "浴室场景", Category = "bathroom", IsBuiltin = true,
                Description = "产品在明亮的浴室环境中", Style = "realistic",
                PromptTemplate = "一个{product_name}在明亮的浴室环境中,清新干净,现代简约,产品特写,真实拍摄效果" },
            new() { Id = Guid.NewGuid(), Name = "户外自然场景", Category = "outdoor", IsBuiltin = true,
                Description = "产品在户外自然环境中", Style = "realistic",
                PromptTemplate = "一个{product_name}在户外自然环境中,阳光充足,清新自然风格,生活方式摄影,高质量" },
            new() { Id = Guid.NewGuid(), Name = "餐厅餐桌场景", Category = "dining", IsBuiltin = true,
                Description = "产品在精致的餐厅餐桌上", Style = "realistic",
                PromptTemplate = "一个{product_name}在精致的餐厅餐桌上,温馨用餐氛围,暖色调光线,美食摄影风格" },
            new() { Id = Guid.NewGuid(), Name = "书房书架场景", Category = "study", IsBuiltin = true,
                Description = "产品在木质书架上", Style = "realistic",
                PromptTemplate = "一个{product_name}在木质书架上,文艺复古风格,柔和灯光,静物摄影,高质感" },
            new() { Id = Guid.NewGuid(), Name = "阳台休闲场景", Category = "balcony", IsBuiltin = true,
                Description = "产品在阳光明媚的阳台上", Style = "realistic",
                PromptTemplate = "一个{product_name}在阳光明媚的阳台上,轻松惬意,绿植点缀,生活气息,自然光摄影" },
            new() { Id = Guid.NewGuid(), Name = "玄关入口场景", Category = "entrance", IsBuiltin = true,
                Description = "产品在现代家居玄关处", Style = "realistic",
                PromptTemplate = "一个{product_name}在现代家居玄关处,简约大气,入门第一印象,高格调摄影" },
        };

        db.ImageTemplates.AddRange(templates);

        var copywritingTemplates = new List<CopywritingTemplate>
        {
            new() { Id = Guid.NewGuid(), Name = "痛点型文案", TemplateType = "pain_point", IsBuiltin = true,
                Content = "你是一个抖音带货文案专家。请为{product_name}写一个痛点型文案。\n\n要求:\n1. 开头用情绪词抓住眼球(如\"绝了\"\"救命\")\n2. 描述用户痛点场景\n3. 说明产品如何解决痛点\n4. 突出价格优势\n5. 结尾引导行动\n\n产品信息:\n- 名称: {product_name}\n- 价格: {price}\n- 卖点: {selling_points}\n\n请生成3个不同版本的文案,用\"---\"分隔。",
                Variables = new[] { "product_name", "price", "selling_points" } },
            new() { Id = Guid.NewGuid(), Name = "性价比型文案", TemplateType = "value", IsBuiltin = true,
                Content = "你是一个抖音带货文案专家。请为{product_name}写一个性价比型文案。\n\n要求:\n1. 强调价格优势\n2. 对比同类产品\n3. 说明质量不打折\n4. 制造紧迫感\n\n产品信息:\n- 名称: {product_name}\n- 价格: {price}\n- 卖点: {selling_points}\n\n请生成3个不同版本的文案,用\"---\"分隔。",
                Variables = new[] { "product_name", "price", "selling_points" } },
            new() { Id = Guid.NewGuid(), Name = "场景代入型文案", TemplateType = "scenario", IsBuiltin = true,
                Content = "你是一个抖音带货文案专家。请为{product_name}写一个场景代入型文案。\n\n要求:\n1. 描绘理想生活场景\n2. 产品如何提升生活品质\n3. 情感共鸣结尾\n\n产品信息:\n- 名称: {product_name}\n- 价格: {price}\n- 卖点: {selling_points}\n\n请生成3个不同版本的文案,用\"---\"分隔。",
                Variables = new[] { "product_name", "price", "selling_points" } },
        };

        db.CopywritingTemplates.AddRange(copywritingTemplates);
        await db.SaveChangesAsync();
    }
}
