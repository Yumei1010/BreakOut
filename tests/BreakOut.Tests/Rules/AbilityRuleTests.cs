using BreakOut.scripts.rules.ability;
using BreakOut.scripts.rules.run;

namespace BreakOut.Tests;

/// <summary>
///     板能力规则测试。
/// </summary>
public class AbilityRuleTests
{
    [Fact]
    public void TryDash_冷却中不可用()
    {
        Assert.Equal(AbilityResult.NotReady, AbilityRule.TryDash(canDash: false));
        Assert.Equal(AbilityResult.Success, AbilityRule.TryDash(canDash: true));
    }

    [Fact]
    public void TryLaser_未满能量_拒绝()
    {
        var run = new RunState();
        run.OnBrickHit(); // 10 能量

        Assert.Equal(AbilityResult.InsufficientEnergy, AbilityRule.TryLaser(run));
    }

    [Fact]
    public void TryLaser_满能量_允许()
    {
        var run = new RunState();
        for (var i = 0; i < 10; i++)
        {
            run.OnBrickHit();
        }

        Assert.Equal(AbilityResult.Success, AbilityRule.TryLaser(run));
    }

    [Fact]
    public void TryAttract_按帧消耗能量()
    {
        var run = new RunState();
        run.AddEnergy(50f);

        var ok = AbilityRule.TryAttract(run, deltaSeconds: 1f); // 耗 5

        Assert.True(ok);
        Assert.Equal(45f, run.Energy);
    }

    [Fact]
    public void TryAttract_能量不足_拒绝且不扣减()
    {
        var run = new RunState();
        run.AddEnergy(2f);

        var ok = AbilityRule.TryAttract(run, deltaSeconds: 1f); // 需 5 > 2

        Assert.False(ok);
        Assert.Equal(2f, run.Energy);
    }

    [Fact]
    public void TryAttract_持续吸引_能量耗尽后拒绝()
    {
        var run = new RunState();
        run.AddEnergy(12f);

        Assert.True(AbilityRule.TryAttract(run, 1f)); // 12→7
        Assert.True(AbilityRule.TryAttract(run, 1f)); // 7→2
        Assert.False(AbilityRule.TryAttract(run, 1f)); // 2<5 拒绝
        Assert.Equal(2f, run.Energy);
    }
}

/// <summary>
///     Magnet 新能力测试（免费召回 + 冷却）。
/// </summary>
public class MagnetAbilityTests
{
    [Fact]
    public void TryMagnet_冷却中不可用()
    {
        Assert.Equal(AbilityResult.NotReady, AbilityRule.TryMagnet(canUse: false));
        Assert.Equal(AbilityResult.Success, AbilityRule.TryMagnet(canUse: true));
    }

    [Fact]
    public void TryMagnet_免费不消耗能量()
    {
        var run = new RunState();
        run.AddEnergy(50f);

        Assert.Equal(AbilityResult.Success, AbilityRule.TryMagnet(canUse: true));
        Assert.Equal(50f, run.Energy); // 免费能力能量不变
    }
}
