using BreakOut.scripts.rules.brick;
using BreakOut.scripts.rules.common;

namespace BreakOut.Tests;

/// <summary>
///     拓展砖种测试：Metal（高血量免疫连锁）与 Rainbow（受击循环变色）。
/// </summary>
public class ExtendedBrickTests
{
    [Fact]
    public void Metal砖_初始四血()
    {
        var brick = new Brick(BrickType.Metal, BrickSize.Long);

        Assert.Equal(4, brick.Health);
        Assert.True(brick.ImmuneToChain);
    }

    [Fact]
    public void Metal砖_受击三次未毁_第四次摧毁()
    {
        var brick = new Brick(BrickType.Metal, BrickSize.Long);

        Assert.False(brick.Damage(1));
        Assert.False(brick.Damage(1));
        Assert.False(brick.Damage(1));
        Assert.True(brick.Damage(1));
        Assert.True(brick.IsDestroyed);
    }

    [Fact]
    public void Metal砖_免疫爆炸连锁()
    {
        var field = new BrickField(explosionRadius: 300f);
        var bomb = new Brick(BrickType.Explosive, BrickSize.Small);
        var metal = new Brick(BrickType.Metal, BrickSize.Small);
        var normal = new Brick(BrickType.One, BrickSize.Small);
        field.Add(bomb, new Vec2(0, 0));
        field.Add(metal, new Vec2(50, 0));   // 爆炸半径内
        field.Add(normal, new Vec2(100, 0)); // 爆炸半径内

        var results = field.HitBrick(bomb, 1);

        Assert.True(normal.IsDestroyed, "普通砖应被连锁摧毁");
        Assert.False(metal.IsDestroyed, "金属砖应免疫连锁");
    }

    [Fact]
    public void Metal砖_仍可被直接命中摧毁()
    {
        var field = new BrickField(explosionRadius: 300f);
        var metal = new Brick(BrickType.Metal, BrickSize.Small);
        field.Add(metal, new Vec2(0, 0));

        var results = field.HitBrick(metal, 10);

        Assert.True(metal.IsDestroyed, "直接高伤命中应可摧毁金属砖");
    }

    [Fact]
    public void Rainbow砖_受击循环变色()
    {
        // 初始视觉（hits=0, seed=0 → Two）
        var first = new Brick(BrickType.Rainbow, BrickSize.Small).VisualType;

        // 重建不同 hit 次数的状态验证循环
        var h1 = new Brick(BrickType.Rainbow, BrickSize.Small);
        h1.Damage(1);
        var afterOneHit = h1.VisualType;

        var h2 = new Brick(BrickType.Rainbow, BrickSize.Small);
        h2.Damage(2);
        var afterTwoHits = h2.VisualType;

        // 三态互不相同且都在 Two/Three/One 中（Rainbow 受击循环）
        Assert.Contains(first, new[] { BrickType.Two, BrickType.Three, BrickType.One });
        Assert.Contains(afterOneHit, new[] { BrickType.Two, BrickType.Three, BrickType.One });
        Assert.Contains(afterTwoHits, new[] { BrickType.Two, BrickType.Three, BrickType.One });
        Assert.NotEqual(afterOneHit, afterTwoHits);
    }

    [Fact]
    public void Rainbow砖_血量为1_受击即毁()
    {
        var brick = new Brick(BrickType.Rainbow, BrickSize.Small);

        var destroyed = brick.Damage(1);

        Assert.True(destroyed);
        Assert.Equal(0, brick.Health); // 单血砖直接摧毁
    }
}
