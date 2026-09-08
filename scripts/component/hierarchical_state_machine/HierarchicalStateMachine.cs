using BreakOut.scripts.component.state_machine;

namespace BreakOut.scripts.component.hierarchical_state_machine;

/// <summary>
///     分层状态机核心类。
///     由一系列状态和它们之间的转换组成，任意时刻只有一个"当前状态"。
///     <para>
///         分层体现在：某个状态可以挂一个子状态机（<c>AttachSubMachine</c>）。
///         进入该状态时自动进入子状态机的初始状态；每帧更新递归到最深层；
///         退出时自底向上退出（先子后父）。
///     </para>
///     <para>
///         使用示例（角色 AI：移动 / 战斗[近战|远程]）：
///         <code>
///         var fsm = new HierarchicalStateMachine()
///             .AddState(move)
///             .AddState(fight)
///             .AddTransition(move, fight, new BoolCondition(() =&gt; hasTarget))
///             .AddTransition(fight, move, new BoolCondition(() =&gt; !hasTarget))
///             .AttachSubMachine(fight, new HierarchicalStateMachine()
///                 .AddState(melee)
///                 .AddState(ranged)
///                 .AddTransition(melee, ranged, new BoolCondition(() =&gt; distanceFar)));
///         fsm.Start();
///         </code>
///     </para>
/// </summary>
public sealed class HierarchicalStateMachine
{
    /// <summary>
    ///     当前状态（本机直接持有的状态；若当前状态挂有活动子机，则为 null，活动状态在子机中）。
    /// </summary>
    public IState? CurrentState => _current;

    /// <summary>
    ///     当前处于活动状态的最深层状态机（本机或递归的子机）。
    /// </summary>
    public HierarchicalStateMachine ActiveMachine =>
        _current is not null && _subMachines.TryGetValue(_current, out var sub) && sub._current is not null
            ? sub.ActiveMachine
            : this;

    /// <summary>
    ///     父状态机；顶级状态机（未被嵌套）时为 null。
    /// </summary>
    public HierarchicalStateMachine? Parent { get; private set; }

    /// <summary>
    ///     当前状态挂载的子状态机；当前状态没有子状态机时为 null。
    /// </summary>
    public HierarchicalStateMachine? SubMachine =>
        _current is not null && _subMachines.TryGetValue(_current, out var sub) ? sub : null;

    private readonly List<IState> _states = new();
    private readonly List<Transition> _transitions = new();
    private readonly Dictionary<IState, HierarchicalStateMachine> _subMachines = new();
    private IState? _current;

    /// <summary>
    ///     注册一个状态到本状态机。重复注册同一状态会被忽略。
    /// </summary>
    /// <param name="state">要注册的状态</param>
    /// <returns>本状态机（支持链式调用）</returns>
    public HierarchicalStateMachine AddState(IState state)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));
        if (!_states.Contains(state))
            _states.Add(state);
        return this;
    }

    /// <summary>
    ///     添加一条转换规则：当前处于 <paramref name="from"/> 且 <paramref name="condition"/> 满足时切换到 <paramref name="to"/>。
    ///     同一状态的多个转换按添加顺序检查，第一个满足条件的生效。
    /// </summary>
    /// <param name="from">来源状态</param>
    /// <param name="to">目标状态</param>
    /// <param name="condition">转换条件</param>
    /// <returns>本状态机（支持链式调用）</returns>
    public HierarchicalStateMachine AddTransition(IState from, IState to, ITransitionCondition condition)
    {
        _transitions.Add(new Transition(from, to, condition));
        return this;
    }

    /// <summary>
    ///     给某个状态挂载子状态机：进入该状态时自动进入 <paramref name="sub"/> 的初始状态，
    ///     每帧更新递归到子状态机，退出该状态时先退出子状态机。
    /// </summary>
    /// <param name="state">要挂载子状态机的父状态</param>
    /// <param name="sub">子状态机</param>
    /// <returns>本状态机（支持链式调用）</returns>
    public HierarchicalStateMachine AttachSubMachine(IState state, HierarchicalStateMachine sub)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));
        if (sub is null)
            throw new ArgumentNullException(nameof(sub));

        _subMachines[state] = sub;
        sub.Parent = this;
        return this;
    }

    /// <summary>
    ///     进入状态机：进入初始状态（第一个注册的状态）。
    ///     若初始状态挂有子状态机，则递归进入其初始状态。
    /// </summary>
    public void Start()
    {
        if (_states.Count == 0)
            return;

        // ChangeState 内部会调用 Enter（含子状态机的递归进入）
        ChangeState(_states[0]);
    }

    /// <summary>
    ///     停止状态机：自底向上退出当前状态（先子后父）并清空当前状态。
    /// </summary>
    public void Stop()
    {
        ExitCurrentState();
    }

    /// <summary>
    ///     每帧更新：若当前状态挂有子状态机则递归更新子状态机，否则更新当前状态；
    ///     随后检查本机转换。
    /// </summary>
    /// <param name="delta">距上一帧的时间（秒）</param>
    public void Process(double delta)
    {
        if (_current is null)
            return;

        if (_subMachines.TryGetValue(_current, out var sub))
            sub.Process(delta);
        else
            _current.Process(delta);

        TryTransition();
    }

    private void TryTransition()
    {
        if (_current is null)
            return;

        foreach (var transition in _transitions)
        {
            if (!transition.ShouldTransition(_current))
                continue;

            ChangeState(transition.To);
            return;
        }
    }

    private void ChangeState(IState next)
    {
        ExitCurrentState();

        _current = next;
        _current.Enter();

        // 进入新状态后，若它挂有子状态机，则递归进入子状态机的初始状态
        if (_subMachines.TryGetValue(next, out var sub))
            sub.Start();
    }

    private void ExitCurrentState()
    {
        if (_current is null)
            return;

        // 先退出子状态机（自底向上退出），再退出父状态
        if (_subMachines.TryGetValue(_current, out var sub))
            sub.Stop();

        _current.Exit();
        _current = null;
    }
}
