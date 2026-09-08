using BreakOut.scripts.rules.bump;

namespace BreakOut.scripts.cqrs.bump.@event;

/// <summary>
///     bump 时机判定事件（驱动表现：飘字/粒子/音效/统计）。
/// </summary>
/// <param name="Grade">判定等级。</param>
public sealed record BumpJudgedEvent(BumpGrade Grade);
