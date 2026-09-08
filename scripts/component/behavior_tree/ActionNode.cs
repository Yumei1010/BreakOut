using GFramework.Core.SourceGenerators.Abstractions.Logging;
using Godot;
using BreakOut.scripts.enums.behavior_tree;

namespace BreakOut.scripts.component.behavior_tree;

/// <summary>
///     动作节点：执行一个具体动作，是行为树的叶子节点。
///     支持两种绑定方式（二选一）：
///     <list type="bullet">
///         <item><description>在编辑器中把 <see cref="Action"/> 设为任意节点的某个方法（返回 int 状态 / bool / void）</description></item>
///         <item><description>通过 <see cref="SetAction"/> 注入 C# 委托</description></item>
///     </list>
/// </summary>
/// <example>
///     例如"攻击目标"、"走向门口"、"播放动画" 都是动作：
///     <code>
///     var attack = new ActionNode();
///     attack.SetAction(() =&gt;
///     {
///         if (IsTargetInRange()) return NodeStatus.Success;
///         MoveToward(target);
///         return NodeStatus.Running; // 正在移动，下一帧继续
///     });
///     </code>
/// </example>
[Log]
public partial class ActionNode : BehaviorNode
{
    private Func<NodeStatus>? _delegateAction;

    /// <summary>
    ///     要调用的 Godot 方法（可在编辑器绑定，或代码调用 <see cref="SetAction"/> 注入委托）。
    /// </summary>
    [Export]
    public Callable Action { get; set; }

    /// <summary>
    ///     通过 C# 委托注入动作逻辑。
    /// </summary>
    /// <param name="action">动作委托，返回执行结果（成功 / 失败 / 执行中）</param>
    public void SetAction(Func<NodeStatus> action)
    {
        _delegateAction = action;
    }

    /// <summary>
    ///     把 Godot 方法的返回值转换为行为树执行结果。
    ///     <list type="bullet">
    ///         <item><description>int：按 <see cref="NodeStatus"/> 枚举值解析（0 成功 / 1 失败 / 2 执行中）</description></item>
    ///         <item><description>bool：true 成功 / false 失败</description></item>
    ///         <item><description>无返回值（void）：视为成功</description></item>
    ///     </list>
    /// </summary>
    /// <param name="result">Godot 方法调用返回值</param>
    /// <returns>行为树执行结果</returns>
    protected static NodeStatus ToStatus(Variant result)
    {
        switch (result.VariantType)
        {
            case Variant.Type.Int:
                var value = result.AsInt32();
                return Enum.IsDefined(typeof(NodeStatus), value) ? (NodeStatus)value : NodeStatus.Failure;
            case Variant.Type.Bool:
                return result.AsBool() ? NodeStatus.Success : NodeStatus.Failure;
            case Variant.Type.Nil:
            default:
                return NodeStatus.Success;
        }
    }

    /// <inheritdoc />
    public override NodeStatus Execute()
    {
        if (_delegateAction is not null)
            return _delegateAction();

        return Action.Method != default ? ToStatus(Action.Call()) : NodeStatus.Failure;
    }
}
