namespace BreakOut.scripts.entities.brick;

/// <summary>
///     砖实体事件订阅：通过 RegisterEvent 订阅 CQRS 事件。
/// </summary>
/// <remarks>
///     当前砖的受击/摧毁逻辑由 <see cref="OnBallHit"/> 直接调用 GameRoot 处理，
///     暂无可独立订阅的 CQRS 事件；后续如需订阅（如连锁动画事件）在此扩展。
/// </remarks>
public partial class BrickView
{
}
