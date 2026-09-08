using BreakOut.scripts.component.state_machine;

namespace BreakOut.scripts.component.hierarchical_state_machine;

/// <summary>
///     状态转换条件接口。
///     由用户实现：告诉状态机"我现在想不想从当前状态切走"。
/// </summary>
public interface ITransitionCondition
{
    /// <summary>
    ///     判断是否满足转换条件（满足则状态机执行转换）。
    /// </summary>
    /// <returns>满足转换条件时返回 true</returns>
    bool ShouldTransition();
}
