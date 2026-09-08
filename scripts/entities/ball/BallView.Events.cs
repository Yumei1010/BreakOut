namespace BreakOut.scripts.entities.ball;

/// <summary>
///     球实体事件订阅：通过 RegisterEvent 订阅 CQRS 事件。
/// </summary>
/// <remarks>
///     当前球的碰撞/bump 逻辑由 _PhysicsProcess 直接处理并调用 GameRoot，
///     暂无可独立订阅的 CQRS 事件；后续如需（如事件驱动碰撞反馈）在此扩展。
/// </remarks>
public partial class BallView
{
}
