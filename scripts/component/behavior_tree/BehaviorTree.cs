using GFramework.Core.SourceGenerators.Abstractions.Logging;
using Godot;
using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     行为树根节点：挂到场景任意位置，自动每帧驱动整棵子树。
///     场景树里它下面的所有 <see cref="BehaviorNode"/> 构成一棵行为树，根节点执行结果可通过 <see cref="LastStatus"/> 查询。
/// </summary>
/// <example>
///     <code>
///     // 场景树（或代码 AddChild）：
///     // BehaviorTree (本节点，自动每帧 Tick)
///     // └── SelectorNode
///     //     ├── SequenceNode
///     //     │   ├── ConditionNode (有目标?)
///     //     │   └── ActionNode (攻击)
///     //     └── ActionNode (巡逻)
///     </code>
/// </example>
[Log]
public partial class BehaviorTree : BehaviorNode
{
    /// <summary>
    ///     是否自动逐帧驱动（true 时每帧执行一次，false 时可手动调用 <see cref="Tick"/>）。
    /// </summary>
    [Export]
    public bool AutoTick { get; set; } = true;

    /// <summary>
    ///     上一次执行结果；未执行过时为 null。
    /// </summary>
    public NodeStatus? LastStatus { get; private set; }

    /// <summary>
    ///     执行一帧：从本节点（根）开始评估整棵子树。
    /// </summary>
    /// <returns>根节点执行结果</returns>
    public NodeStatus Tick()
    {
        LastStatus = Execute();
        return LastStatus.Value;
    }

    /// <summary>
    ///     自动驱动模式下的逐帧更新。
    /// </summary>
    public override void _Process(double delta)
    {
        if (AutoTick)
            Tick();
    }

    /// <inheritdoc />
    public override NodeStatus Execute() => ChildNodes.FirstOrDefault()?.Execute() ?? NodeStatus.Success;
}
