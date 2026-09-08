namespace BreakOut.scripts.component.state_machine;

/// <summary>
///     状态契约，所有状态必须实现此接口。
///     一个状态有三个生命周期时机：进入、每帧更新、退出。
/// </summary>
public interface IState
{
    /// <summary>
    ///     进入状态时调用一次（例如：切换武器、播放入场动画）。
    /// </summary>
    void Enter();

    /// <summary>
    ///     每帧更新时调用，<paramref name="delta"/> 为距上一帧的时间（秒）。
    /// </summary>
    /// <param name="delta">距上一帧的时间（秒）</param>
    void Process(double delta);

    /// <summary>
    ///     退出状态时调用一次（例如：停止当前动画、释放资源）。
    /// </summary>
    void Exit();
}
