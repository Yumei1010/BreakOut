namespace BreakOut.scripts.component.hierarchical_state_machine;

/// <summary>
///     布尔转换条件：封装一个可随时更新的布尔值，满足时触发转换。
///     这是最常见的转换条件形式（例如："血量低于 30%"、"是否按下了跳跃键"）。
/// </summary>
public sealed class BoolCondition : ITransitionCondition
{
    private readonly Func<bool> _predicate;

    /// <summary>
    ///     创建一个布尔条件。
    /// </summary>
    /// <param name="predicate">每次检查转换时都会调用的判断函数</param>
    public BoolCondition(Func<bool> predicate)
    {
        _predicate = predicate;
    }

    /// <summary>
    ///     判断条件是否满足。
    /// </summary>
    public bool ShouldTransition() => _predicate();
}
