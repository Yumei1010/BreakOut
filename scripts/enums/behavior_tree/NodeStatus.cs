namespace BreakOut.scripts.enums.behavior_tree;

/// <summary>
///     行为树节点执行结果。
///     <list type="bullet">
///         <item><description><see cref="Success"/>：节点执行成功（例如：动作完成、条件满足）</description></item>
///         <item><description><see cref="Failure"/>：节点执行失败（例如：动作无法完成、条件不满足）</description></item>
///         <item><description><see cref="Running"/>：节点执行中，需要多帧才能完成（例如：正在走向目标）</description></item>
///     </list>
/// </summary>
public enum NodeStatus
{
    /// <summary>
    ///     执行成功。
    /// </summary>
    Success,

    /// <summary>
    ///     执行失败。
    /// </summary>
    Failure,

    /// <summary>
    ///     执行中（多帧任务），下一帧会继续执行本节点。
    /// </summary>
    Running,
}
