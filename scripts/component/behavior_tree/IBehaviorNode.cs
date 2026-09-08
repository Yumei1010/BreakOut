using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     行为树节点接口契约。
///     所有行为树节点（动作、条件、顺序、选择、根）都实现此接口，
///     通过 <see cref="Execute"/> 统一驱动，复合节点递归调用子节点。
/// </summary>
public interface IBehaviorNode
{
    /// <summary>
    ///     执行本节点，返回执行结果。
    ///     <para>返回 <see cref="NodeStatus.Running"/> 表示任务需要多帧完成，下一帧会继续执行本节点。</para>
    /// </summary>
    /// <returns>成功 / 失败 / 执行中</returns>
    NodeStatus Execute();
}
