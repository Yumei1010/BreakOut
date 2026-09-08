namespace BreakOut.scripts.domain.ability;

/// <summary>
///     板能力类型。
/// </summary>
public enum AbilityType
{
    /// <summary>
    ///     冲刺：短时间水平爆发移动（无能量消耗）。
    /// </summary>
    Dash,

    /// <summary>
    ///     终极激光：需满能量，发射后清空能量，贯穿摧毁砖列。
    /// </summary>
    Laser,

    /// <summary>
    ///     吸引：按住时持续消耗能量将球拉向板。
    /// </summary>
    Attract
}
