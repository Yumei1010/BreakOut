using BreakOut.scripts.component.state_machine;

namespace BreakOut.scripts.component.hierarchical_state_machine;

/// <summary>
///     状态转换定义：从 <see cref="From"/> 状态在满足 <see cref="Condition"/> 时切换到 <see cref="To"/> 状态。
///     一个状态的多个转换按添加顺序检查，第一个满足条件的生效。
/// </summary>
/// <param name="from">来源状态</param>
/// <param name="to">目标状态</param>
/// <param name="condition">转换条件</param>
public sealed class Transition(IState from, IState to, ITransitionCondition condition)
{
    /// <summary>
    ///     来源状态（转换发生时必须是当前状态）。
    /// </summary>
    public IState From { get; } = from;

    /// <summary>
    ///     目标状态。
    /// </summary>
    public IState To { get; } = to;

    /// <summary>
    ///     转换条件。
    /// </summary>
    public ITransitionCondition Condition { get; } = condition;

    /// <summary>
    ///     检查当前状态是否满足此转换。
    /// </summary>
    /// <param name="current">当前状态</param>
    /// <returns>当前状态为此转换的来源状态且条件满足时返回 true</returns>
    public bool ShouldTransition(IState current)
    {
        return ReferenceEquals(current, From) && Condition.ShouldTransition();
    }
}
