using BreakOut.scripts.rules.run;
using BreakOut.scripts.system.run;

namespace BreakOut.Tests;

/// <summary>
///     对局流程系统测试：状态流转决策。
/// </summary>
public class RunFlowSystemTests
{
    [Fact]
    public void OnBallLost_三次落底_游戏结束()
    {
        var flow = new RunFlowSystem();
        flow.StartBall();

        Assert.False(flow.OnBallLost().IsGameOver);
        Assert.False(flow.OnBallLost().IsGameOver);
        var outcome = flow.OnBallLost();

        Assert.True(outcome.IsGameOver);
        Assert.Equal(RunPhase.GameOver, outcome.NextPhase);
        Assert.Equal(0, outcome.RemainingHealth);
    }

    [Fact]
    public void OnBallLost_还有生命_回到待发球()
    {
        var flow = new RunFlowSystem();
        flow.StartBall();

        var outcome = flow.OnBallLost();

        Assert.False(outcome.IsGameOver);
        Assert.Equal(RunPhase.Ready, outcome.NextPhase);
        Assert.Equal(2, outcome.RemainingHealth);
    }

    [Fact]
    public void StartBall_进入进行中()
    {
        var flow = new RunFlowSystem();

        flow.StartBall();

        Assert.Equal(RunPhase.Playing, flow.Run.Phase);
    }

    [Fact]
    public void OnLevelCleared_进入过关()
    {
        var flow = new RunFlowSystem();
        flow.StartBall();

        flow.OnLevelCleared();

        Assert.Equal(RunPhase.StageClear, flow.Run.Phase);
    }

}
