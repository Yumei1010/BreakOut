using GFramework.Core.SourceGenerators.Abstractions.Logging;
using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     顺序节点（Sequence）：从左到右依次执行子节点。
///     遇到失败立即整体失败；全部成功才整体成功；遇到执行中则暂停，下一帧从该子节点继续。
///     <para>典型用法：<b>"先做 A，再做 B，最后做 C"</b>（其中一步失败则全不做）。</para>
/// </summary>
/// <example>
///     <code>
///     // 场景树（或代码 AddChild）：
///     // SequenceNode
///     // ├── ConditionNode：有弹药？  （闸门：没弹药整体失败）
///     // ├── ActionNode：装弹
///     // └── ActionNode：射击
///     </code>
/// </example>
[Log]
public partial class SequenceNode : BehaviorNode
{
    private int _currentIndex;

    /// <inheritdoc />
    public override NodeStatus Execute()
    {
        // 从上次暂停的位置继续（避免每帧从头重跑已完成/进行中的子节点）
        while (_currentIndex < ChildNodes.Count)
        {
            var status = ChildNodes[_currentIndex].Execute();

            if (status == NodeStatus.Failure)
            {
                // 某一步失败，整体失败，下次从头开始
                _currentIndex = 0;
                return NodeStatus.Failure;
            }

            if (status == NodeStatus.Running)
            {
                // 当前子节点执行中，记住位置，下一帧继续
                return NodeStatus.Running;
            }

            // 当前子节点成功，进入下一个
            _currentIndex++;
        }

        // 全部成功
        _currentIndex = 0;
        return NodeStatus.Success;
    }
}
