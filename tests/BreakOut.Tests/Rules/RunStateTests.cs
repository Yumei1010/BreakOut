using BreakOut.scripts.rules.run;

namespace BreakOut.Tests;

/// <summary>
///     对局状态测试：生命/能量/阶段流转。
/// </summary>
public class RunStateTests
{
    [Fact]
    public void 初始状态_三命零能量待发球()
    {
        var run = new RunState();

        Assert.Equal(3, run.Health);
        Assert.Equal(0f, run.Energy);
        Assert.Equal(RunPhase.Ready, run.Phase);
    }

    [Fact]
    public void 球落底_扣一命回待发球()
    {
        var run = new RunState();
        run.StartBall();

        var dead = run.OnBallLost();

        Assert.False(dead);
        Assert.Equal(2, run.Health);
        Assert.Equal(RunPhase.Ready, run.Phase);
    }

    [Fact]
    public void 三次落底_进入GameOver()
    {
        var run = new RunState();
        run.StartBall();

        run.OnBallLost();
        run.OnBallLost();
        var dead = run.OnBallLost();

        Assert.True(dead);
        Assert.Equal(0, run.Health);
        Assert.Equal(RunPhase.GameOver, run.Phase);
    }

    [Fact]
    public void 撞砖十次_能量满触发回调()
    {
        var triggered = 0;
        var run = new RunState(onEnergyFull: () => triggered++);

        for (var i = 0; i < 10; i++)
        {
            run.OnBrickHit();
        }

        Assert.Equal(100f, run.Energy);
        Assert.Equal(1, triggered);
    }

    [Fact]
    public void 能量砖摧毁_一次补满()
    {
        var run = new RunState();
        run.OnBrickHit(); // 10

        run.OnEnergyBrickDestroyed(); // +100 → clamp 100

        Assert.Equal(100f, run.Energy);
    }

    [Fact]
    public void 满能量后消耗_可再次触发满能量回调()
    {
        var triggered = 0;
        var run = new RunState(onEnergyFull: () => triggered++);
        for (var i = 0; i < 10; i++)
        {
            run.OnBrickHit();
        }
        Assert.Equal(1, triggered);

        run.SpendEnergy(100f);
        for (var i = 0; i < 10; i++)
        {
            run.OnBrickHit();
        }

        Assert.Equal(100f, run.Energy);
        Assert.Equal(2, triggered);
    }

    [Fact]
    public void 能量不会低于零()
    {
        var run = new RunState();
        run.AddEnergy(5f);

        run.SpendEnergy(999f);

        Assert.Equal(0f, run.Energy);
    }

    [Fact]
    public void 关卡清除_进入StageClear()
    {
        var run = new RunState();
        run.StartBall();

        run.OnLevelCleared();

        Assert.Equal(RunPhase.StageClear, run.Phase);
    }

    [Fact]
    public void Reset_恢复初始()
    {
        var run = new RunState();
        run.StartBall();
        run.OnBallLost();
        run.OnEnergyBrickDestroyed();

        run.Reset();

        Assert.Equal(3, run.Health);
        Assert.Equal(0f, run.Energy);
        Assert.Equal(RunPhase.Ready, run.Phase);
    }
}
