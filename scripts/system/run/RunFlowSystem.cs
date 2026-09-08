using BreakOut.scripts.rules.run;

namespace BreakOut.scripts.system.run;

/// <summary>
///     对局流程系统：集中发球/落底/过关的状态流转决策（纯 C# 可测）。
/// </summary>
/// <remarks>
///     持有对局状态 RunState，把"球落底/发球/过关"等事件转为阶段决策，
///     供视图层据此驱动表现（死亡弹层/重挂球/下一关）。
/// </remarks>
public sealed class RunFlowSystem
{
    /// <summary>
    ///     获取对局状态。
    /// </summary>
    /// <summary>
    ///     获取对局状态。
    /// </summary>
    public RunState Run { get; } = new();

    private double _elapsedTime;

    /// <summary>
    ///     球落底：扣命并返回本次落底的阶段结果。
    /// </summary>
    public BallLostOutcome OnBallLost()
    {
        var dead = Run.OnBallLost();
        return new BallLostOutcome(
            Run.Health,
            dead ? RunPhase.GameOver : RunPhase.Ready,
            dead);
    }

    /// <summary>
    ///     发球（板将球打出）。
    /// </summary>
    public void StartBall()
    {
        Run.StartBall();
    }

    /// <summary>
    ///     关卡全部砖清除。
    /// </summary>
    public void OnLevelCleared()
    {
        Run.OnLevelCleared();
    }

    /// <summary>
    ///     累计对局时间（Playing 阶段）。
    /// </summary>
    /// <param name="delta">帧时间。</param>
    public void TickTime(double delta)
    {
        if (Run.Phase == RunPhase.Playing)
        {
            _elapsedTime += delta;
        }
    }

    /// <summary>
    ///     重置对局（重开）。
    /// </summary>
    public void Reset()
    {
        _elapsedTime = 0;
        Run.Reset();
    }
}

/// <summary>
///     球落底结果。
/// </summary>
/// <param name="RemainingHealth">剩余生命。</param>
/// <param name="NextPhase">落底后应进入的阶段。</param>
/// <param name="IsGameOver">是否游戏结束。</param>
public sealed record BallLostOutcome(int RemainingHealth, RunPhase NextPhase, bool IsGameOver);
