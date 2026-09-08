using BreakOut.scripts.domain.run;

namespace BreakOut.scripts.cqrs.run.@event;

/// <summary>
///     能量变化事件（驱动 HUD 能量条与终极就绪提示）。
/// </summary>
/// <param name="Energy">当前能量值。</param>
/// <param name="EnergyFull">是否达到满能量。</param>
public sealed record EnergyChangedEvent(float Energy, bool EnergyFull);
