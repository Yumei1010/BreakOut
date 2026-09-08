using BreakOut.scripts.rules.scoring;

namespace BreakOut.scripts.cqrs.scoring.@event;

/// <summary>
///     得分变化事件（驱动 HUD 分数显示与连击表现）。
/// </summary>
/// <param name="Score">当前累计得分。</param>
/// <param name="Combo">当前连击数。</param>
public sealed record ScoreChangedEvent(int Score, int Combo);
