using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using Godot;
using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     行为树节点的抽象基类。
///     所有行为树节点都是 Godot 节点（继承 <see cref="Node"/>），在场景树中拼装层级即组成行为树：
///     叶子节点（动作/条件）做具体事，复合节点（顺序/选择）控制执行逻辑。
/// </summary>
/// <remarks>
///     <para>使用方式：在场景树中拖拽拼装，或代码 <c>AddChild</c> 动态组装。</para>
///     <para>复合节点（<see cref="SequenceNode"/> / <see cref="SelectorNode"/>）的子节点即其执行序列。</para>
/// </remarks>
[Log]
[ContextAware]
public abstract partial class BehaviorNode : Node, IBehaviorNode
{
    /// <summary>
    ///     当前节点的子行为树节点（复合节点用）。
    /// </summary>
    protected IReadOnlyList<BehaviorNode> ChildNodes => GetChildren().OfType<BehaviorNode>().ToList();

    /// <summary>
    ///     执行本节点，返回执行结果。
    /// </summary>
    /// <remarks>
    ///     复合节点执行时会递归调用子节点，整棵树从根节点开始自上而下评估。
    ///     <para>返回 <see cref="NodeStatus.Running"/> 表示任务需要多帧完成，下一帧会继续执行。</para>
    /// </remarks>
    /// <returns>节点执行结果：成功 / 失败 / 执行中</returns>
    public abstract NodeStatus Execute();
}
