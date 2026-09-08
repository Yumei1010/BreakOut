using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BreakOut.scripts.component.action_queue;

/// <summary>
///     动作队列：按序串行执行异步步骤（前一个完成才执行下一个）。
///     契合"按序播放动画/连锁效果"场景：把动画步骤排入队列，自动逐个执行。
/// </summary>
/// <remarks>
///     纯逻辑组件，零 Godot / GFramework 依赖，可在单元测试中使用。
///     <para>
///     用法示例：
///     <code>
///     var queue = new ActionQueue();
///     queue.Enqueue(async () =&gt; { await MoveCardToTarget(); });   // 第 1 步：移动卡牌
///     queue.Enqueue(async () =&gt; { await FlipCard(); });            // 第 2 步：翻牌
///     queue.Enqueue(() =&gt; CalculateScore());                       // 第 3 步：计分
///     // 自动串行执行：移动完成 → 翻牌完成 → 计分
///     </code>
///     </para>
/// </remarks>
public sealed class ActionQueue
{
    private readonly Queue<Func<Task>> _pending = new();
    private TaskCompletionSource? _currentStep;
    private bool _isRunning;

    /// <summary>
    ///     队列是否正在执行（有步骤运行中或待执行）。
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    ///     当前待执行步骤数（不含正在执行的）。
    /// </summary>
    public int PendingCount => _pending.Count;

    /// <summary>
    ///     是否为空（无待执行且未在运行）。
    /// </summary>
    public bool IsEmpty => !_isRunning && _pending.Count == 0;

    /// <summary>
    ///     排入一个异步步骤。若队列空闲则立即开始执行，否则等待前序完成。
    /// </summary>
    /// <param name="step">异步步骤，返回的 Task 完成表示该步骤结束</param>
    public void Enqueue(Func<Task> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _pending.Enqueue(step);
        TryRunNext();
    }

    /// <summary>
    ///     清空所有待执行步骤（正在执行的步骤不受影响）。
    /// </summary>
    public void Clear()
    {
        _pending.Clear();
    }

    /// <summary>
    ///     等待当前及后续所有步骤执行完毕。
    /// </summary>
    /// <returns>全部步骤完成时返回的 Task</returns>
    public Task WaitUntilIdleAsync()
    {
        return _currentStep?.Task ?? Task.CompletedTask;
    }

    /// <summary>
    ///     尝试取出并执行下一个步骤（若空闲且有排队）。
    /// </summary>
    private void TryRunNext()
    {
        if (_isRunning)
        {
            return;
        }

        if (_pending.Count == 0)
        {
            return;
        }

        _isRunning = true;
        _currentStep = new TaskCompletionSource();
        _ = RunStepsAsync();
    }

    /// <summary>
    ///     循环取出步骤串行执行，直到队列清空。
    /// </summary>
    private async Task RunStepsAsync()
    {
        try
        {
            while (_pending.Count > 0)
            {
                var step = _pending.Dequeue();
                await step().ConfigureAwait(false);
            }
        }
        finally
        {
            _isRunning = false;
            _currentStep?.TrySetResult();
            _currentStep = null;
        }
    }
}
