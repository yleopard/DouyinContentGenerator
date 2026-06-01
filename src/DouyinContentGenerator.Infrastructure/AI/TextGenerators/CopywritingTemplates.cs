namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

public static class CopywritingTemplates
{
    public static readonly Dictionary<string, string> BuiltInTemplates = new()
    {
        ["pain_point"] = @"你是一个抖音带货文案专家。请为{product_name}写一个痛点型文案。

要求:
1. 开头用情绪词抓住眼球(如""绝了""救命"")
2. 描述用户痛点场景
3. 说明产品如何解决痛点
4. 突出价格优势
5. 结尾引导行动

产品信息:
- 名称: {product_name}
- 价格: {price}
- 卖点: {selling_points}

请生成3个不同版本的文案,用""---""分隔。",

        ["value"] = @"你是一个抖音带货文案专家。请为{product_name}写一个性价比型文案。

要求:
1. 强调价格优势
2. 对比同类产品
3. 说明质量不打折
4. 制造紧迫感

产品信息:
- 名称: {product_name}
- 价格: {price}
- 卖点: {selling_points}

请生成3个不同版本的文案,用""---""分隔。",

        ["scenario"] = @"你是一个抖音带货文案专家。请为{product_name}写一个场景代入型文案。

要求:
1. 描绘理想生活场景
2. 产品如何提升生活品质
3. 情感共鸣结尾

产品信息:
- 名称: {product_name}
- 价格: {price}
- 卖点: {selling_points}

请生成3个不同版本的文案,用""---""分隔。"
    };
}
