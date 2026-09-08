using BreakOut.scripts.rules.brick;
using BreakOut.scripts.rules.common;

namespace BreakOut.Tests;

/// <summary>
///     砖块规则测试：血量/受击/爆炸连锁/能量砖。
/// </summary>
public class BrickTests
{
    [Theory]
    [InlineData(BrickType.One, 1)]
    [InlineData(BrickType.Two, 2)]
    [InlineData(BrickType.Three, 3)]
    [InlineData(BrickType.Explosive, 1)]
    [InlineData(BrickType.Energy, 1)]
    public void 砖种初始血量对照原版(BrickType type, int expected)
    {
        var brick = new Brick(type, BrickSize.Small);

        Assert.Equal(expected, brick.Health);
    }

    [Fact]
    public void 两血砖受击一次未摧毁_视觉降级为One()
    {
        var brick = new Brick(BrickType.Two, BrickSize.Small);

        var destroyed = brick.Damage(1);

        Assert.False(destroyed);
        Assert.False(brick.IsDestroyed);
        Assert.Equal(1, brick.Health);
        Assert.Equal(BrickType.One, brick.VisualType);
    }

    [Fact]
    public void 三血砖受击一次_剩余两血视觉为Two()
    {
        var brick = new Brick(BrickType.Three, BrickSize.Small);

        brick.Damage(1);

        Assert.Equal(BrickType.Two, brick.VisualType);
        Assert.Equal(2, brick.Health);
    }

    [Fact]
    public void 三血砖受击两次_剩余一血视觉降级One()
    {
        var brick = new Brick(BrickType.Three, BrickSize.Small);
        brick.Damage(1);
        brick.Damage(1);

        Assert.Equal(BrickType.One, brick.VisualType);
        Assert.Equal(1, brick.Health);
    }

    [Fact]
    public void 一血砖受击_立即摧毁()
    {
        var brick = new Brick(BrickType.One, BrickSize.Long);

        var destroyed = brick.Damage(1);

        Assert.True(destroyed);
        Assert.True(brick.IsDestroyed);
    }

    [Fact]
    public void 已摧毁砖再次受击_忽略()
    {
        var brick = new Brick(BrickType.One, BrickSize.Small);
        brick.Damage(1);

        var destroyed = brick.Damage(1);

        Assert.False(destroyed);
    }

    [Fact]
    public void 爆炸砖摧毁_连锁摧毁半径内砖()
    {
        var field = new BrickField(explosionRadius: 200f);
        var bomb = new Brick(BrickType.Explosive, BrickSize.Small);
        var victim = new Brick(BrickType.One, BrickSize.Small);
        var far = new Brick(BrickType.Three, BrickSize.Small);
        field.Add(bomb, new Vec2(0, 0));
        field.Add(victim, new Vec2(50, 0));   // 半径内
        field.Add(far, new Vec2(500, 0));     // 半径外

        var results = field.HitBrick(bomb, 1);

        Assert.True(bomb.IsDestroyed);
        Assert.True(victim.IsDestroyed);
        Assert.False(far.IsDestroyed);
        Assert.Contains(results, r => r.Reason == BrickDestroyReason.BallHit);
        Assert.Contains(results, r => r.Brick == victim && r.Reason == BrickDestroyReason.ChainExplosion);
    }

    [Fact]
    public void 爆炸连锁_触发其他爆炸砖继续扩散()
    {
        var field = new BrickField(explosionRadius: 200f);
        var bombA = new Brick(BrickType.Explosive, BrickSize.Small);
        var bombB = new Brick(BrickType.Explosive, BrickSize.Small);
        var victim = new Brick(BrickType.One, BrickSize.Small);
        field.Add(bombA, new Vec2(0, 0));
        field.Add(bombB, new Vec2(100, 0));   // bombA 半径内
        field.Add(victim, new Vec2(180, 0));  // bombB 半径内、bombA 半径外

        var results = field.HitBrick(bombA, 1);

        Assert.True(bombA.IsDestroyed);
        Assert.True(bombB.IsDestroyed);
        Assert.True(victim.IsDestroyed);
        // bombA(直接) + bombB(连锁) + victim(二次连锁)
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void 能量砖摧毁_触发能量回调()
    {
        var field = new BrickField(explosionRadius: 200f);
        var energy = new Brick(BrickType.Energy, BrickSize.Small);
        field.Add(energy, new Vec2(0, 0));
        var callbackCount = 0;

        field.HitBrick(energy, 1, onEnergyBrickDestroyed: () => callbackCount++);

        Assert.Equal(1, callbackCount);
        Assert.True(energy.IsDestroyed);
    }

    [Fact]
    public void 普通砖摧毁_不触发能量回调()
    {
        var field = new BrickField(explosionRadius: 200f);
        var normal = new Brick(BrickType.One, BrickSize.Small);
        field.Add(normal, new Vec2(0, 0));
        var callbackCount = 0;

        field.HitBrick(normal, 1, onEnergyBrickDestroyed: () => callbackCount++);

        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public void 砖墙存活计数()
    {
        var field = new BrickField(200f);
        field.Add(new Brick(BrickType.One, BrickSize.Small), new Vec2(0, 0));
        var second = new Brick(BrickType.One, BrickSize.Small);
        field.Add(second, new Vec2(10, 0));
        Assert.Equal(2, field.AliveCount);

        field.HitBrick(second, 1);

        Assert.Equal(1, field.AliveCount);
    }
}
