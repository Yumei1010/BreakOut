namespace BreakOut.scripts.entities.ball;

/// <summary>
///     球实体信号桥：Godot 信号 → CQRS 命令/事件。
/// </summary>
/// <remarks>
///     当前球以 CharacterBody2D 物理碰撞为主，碰撞转发在 _PhysicsProcess 直接完成，
///     暂无可桥接的 Godot 信号；后续如需（如动画完成信号）在此扩展。
/// </remarks>
public partial class BallView
{
}
