using BreakOut.scripts.domain.brick;

namespace BreakOut.scripts.cqrs.brick.@event;

/// <summary>
///     砖被摧毁事件（含连锁）。
/// </summary>
/// <param name="DestroyedCount">本次事件摧毁的砖数（含连锁引爆）。</param>
/// <param name="AliveCount">场上剩余普通砖数。</param>
/// <param name="LevelCleared">是否因此通关。</param>
public sealed record BrickDestroyedEvent(int DestroyedCount, int AliveCount, bool LevelCleared);
