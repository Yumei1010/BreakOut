using BreakOut.scripts.domain.brick;

namespace BreakOut.scripts.cqrs.run.@event;

/// <summary>
///     球落底事件：扣减生命并可能需要重置球（规则级状态变化）。
/// </summary>
/// <param name="RemainingHealth">剩余生命。</param>
/// <param name="GameOver">生命是否已归零。</param>
public sealed record BallLostEvent(int RemainingHealth, bool GameOver);
