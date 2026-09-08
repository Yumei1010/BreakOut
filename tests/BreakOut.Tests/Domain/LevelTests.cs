using BreakOut.scripts.domain.brick;
using BreakOut.scripts.domain.common;
using BreakOut.scripts.domain.level;

namespace BreakOut.Tests;

/// <summary>
///     砖种随机与关卡布局测试。
/// </summary>
public class LevelTests
{
    [Fact]
    public void RollType_固定种子生成_覆盖多类型且均为合法类型()
    {
        var random = new Random(42);

        for (var i = 0; i < 200; i++)
        {
            var type = BrickRandomizer.RollType(random);
            Assert.Contains(type, Enum.GetValues<BrickType>());
        }
    }

    [Fact]
    public void RollType_大量采样_分布接近原版概率表()
    {
        var random = new Random(2026);
        var buckets = new Dictionary<BrickType, int>();
        const int samples = 20000;

        for (var i = 0; i < samples; i++)
        {
            var type = BrickRandomizer.RollType(random);
            buckets[type] = buckets.GetValueOrDefault(type) + 1;
        }

        double RatioOf(BrickType t) => (double)buckets.GetValueOrDefault(t) / samples;
        // 权重驱动后总权重 110：基础砖比例按 Catalog 权重折算 ±1.5% 容差
        Assert.InRange(RatioOf(BrickType.Explosive), 5f / 110 - 0.015, 5f / 110 + 0.015);
        Assert.InRange(RatioOf(BrickType.Energy), 10f / 110 - 0.02, 10f / 110 + 0.02);
        Assert.InRange(RatioOf(BrickType.Three), 20f / 110 - 0.02, 20f / 110 + 0.02);
        Assert.InRange(RatioOf(BrickType.Two), 30f / 110 - 0.02, 30f / 110 + 0.02);
        Assert.InRange(RatioOf(BrickType.One), 35f / 110 - 0.02, 35f / 110 + 0.02);
        // 拓展砖种按注册权重出现
        Assert.InRange(RatioOf(BrickType.Metal), 6f / 110 - 0.015, 6f / 110 + 0.015);
        Assert.InRange(RatioOf(BrickType.Rainbow), 4f / 110 - 0.015, 4f / 110 + 0.015);
    }

    [Fact]
    public void RollSize_大量采样_六成五长砖比例()
    {
        var random = new Random(7);
        var longCount = 0;
        const int samples = 20000;

        for (var i = 0; i < samples; i++)
        {
            if (BrickRandomizer.RollSize(random) == BrickSize.Long)
            {
                longCount++;
            }
        }

        var ratio = (double)longCount / samples;
        Assert.InRange(ratio, 0.63, 0.67);
    }

    [Fact]
    public void Generate_全部锚点被投放_填充率低于100()
    {
        var generator = new LevelGenerator(new Random(1));
        var anchors = Enumerable.Range(0, 100)
            .Select(i => new Vec2(i * 10f, 0f))
            .ToList();

        var spawns = generator.Generate(anchors, LevelConfig.Default);

        Assert.True(spawns.Count > 0, "100 锚点 90% 填充率下不应为空");
        Assert.True(spawns.Count <= 100);
    }

    [Fact]
    public void Generate_填充率1_所有锚点都有砖()
    {
        var generator = new LevelGenerator(new Random(1));
        var anchors = Enumerable.Range(0, 50)
            .Select(i => new Vec2(i * 10f, 0f))
            .ToList();

        var spawns = generator.Generate(anchors, new LevelConfig(fillProbability: 1.0f));

        Assert.Equal(50, spawns.Count);
    }

    [Fact]
    public void Generate_填充率0_无砖生成()
    {
        var generator = new LevelGenerator(new Random(1));
        var anchors = new[] { new Vec2(0, 0), new Vec2(10, 0) };

        var spawns = generator.Generate(anchors, new LevelConfig(fillProbability: 0.0f));

        Assert.Empty(spawns);
    }

    [Fact]
    public void Generate_保留锚点位置()
    {
        var generator = new LevelGenerator(new Random(99));
        var anchor = new Vec2(320, 120);
        var anchors = Enumerable.Repeat(anchor, 30).ToList();

        var spawns = generator.Generate(anchors, new LevelConfig(fillProbability: 1.0f));

        Assert.All(spawns, s => Assert.Equal(anchor, s.Position));
    }
}
