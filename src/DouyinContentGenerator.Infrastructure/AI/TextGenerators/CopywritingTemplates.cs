namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

public static class CopywritingTemplates
{
    public static readonly Dictionary<string, string> BuiltInTemplates = new()
    {
        ["pain_point"] = "You are a professional Douyin e-commerce copywriter. Write 3 compelling pain-point marketing copies for the product {product_name}, priced at {price}, with selling points: {selling_points}. Requirements: 1) Eye-catching emotional hook 2) Describe user pain scenario 3) Show how product solves it 4) Highlight price advantage 5) Call to action. Also add 3-5 hashtags like #种草 #好物推荐 at the end. Separate 3 versions with ---.",

        ["value"] = "You are a professional Douyin product reviewer. Write 3 value/comparison marketing copies for {product_name}, priced at {price}, with selling points: {selling_points}. Requirements: 1) Build trust with data 2) Compare with competitors 3) Break down cost (only X per day) 4) Real usage experience 5) Add hashtags #测评 #性价比. Separate 3 versions with ---.",

        ["scenario"] = "You are a Douyin lifestyle blogger. Write 3 lifestyle scenario marketing copies for {product_name}, priced at {price}, with selling points: {selling_points}. Requirements: 1) Beautiful life scene opening 2) Five-sense description 3) Natural product placement 4) Emotional connection 5) Soft call to action. Add hashtags #生活美学 #治愈系好物. Separate 3 versions with ---.",

        ["trending"] = "You are a Douyin trending content strategist. Write 3 viral topic marketing copies for {product_name}, priced at {price}, with selling points: {selling_points}. Requirements: 1) Hook with trending topic 2) Dramatic before/after contrast 3) Interactive closing to drive comments 4) Short punchy sentences. Separate 3 versions with ---."
    };
}
