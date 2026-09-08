namespace BreakOut.scripts.domain.run;

/// <summary>
///     对局阶段。
/// </summary>
public enum RunPhase
{
    /// <summary>
    ///     待发球（球吸附在板上）。
    /// </summary>
    Ready,

    /// <summary>
    ///     对局进行中。
    /// </summary>
    Playing,

    /// <summary>
    ///     玩家死亡（生命归零），等待结算。
    /// </summary>
    GameOver,

    /// <summary>
    ///     关卡清除，等待下一关。
    /// </summary>
    StageClear
}
