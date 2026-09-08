using GFramework.Core.SourceGenerators.Abstractions.Logging;
using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     选择节点（Selector）：从左到右依次尝试子节点，找到第一个成功的执行。
///     遇到成功立即整体成功；全部失败才整体失败；遇到执行中则暂停，下一帧从该子节点继续。
///     <para>典型用法：<b>"优先做 A，不行就做 B，再不行做 C"</b>（回退/兜底逻辑）。</para>
/// </summary>
/// <example>
///     <code>
///     // 场景树（或代码 AddChild）：
///     // SelectorNode
///     // ├── ActionNode：攻击   （优先）
///     // ├── ActionNode：追目标 （回退）
///     // └── ActionNode：巡逻   （兜底）
///     </code>
/// </example>
[Log]
public partial class SelectorNode : BehaviorNode
{
    private int _currentIndex;

    /// <inheritdoc />
    public override NodeStatus Execute()
    {
        // 从上次暂停的位置继续
        while (_currentIndex < ChildNodes.Count)
        {
            var status = ChildNodes[_currentIndex].Execute();

            if (status == NodeStatus.Success)
            {
                // 找到一个成功的，整体成功，下次从头开始
                _currentIndex = 0;
                return NodeStatus.Success;
            }

            if (status == NodeStatus.Running)
            {
                // 当前子节点执行中，记住位置，下一帧继续
                return NodeStatus.Running;
            }

            // 当前子节点失败，尝试下一个
            _currentIndex++;
        }

        // 全部失败
        _currentIndex = 0;
        return NodeStatus.Failure;
    }
}
