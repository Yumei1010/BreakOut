using BreakOut.scripts.component.action_queue;

namespace BreakOut.Tests;

/// <summary>
///     动作队列测试：验证串行执行顺序、运行中入队、清空、等待完成。
/// </summary>
public class ActionQueueTests
{
    /// <summary>
    ///     记录执行顺序的历史。
    /// </summary>
    private readonly List<string> _history = new();

    [Fact]
    public async Task Enqueue_RunsStepsInOrder_Sequentially()
    {
        var queue = new ActionQueue();
        var order = new List<string>();

        queue.Enqueue(async () =>
        {
            order.Add("A-start");
            await Task.Delay(30);
            order.Add("A-end");
        });
        queue.Enqueue(async () =>
        {
            order.Add("B-start");
            await Task.Delay(5);
            order.Add("B-end");
        });

        await queue.WaitUntilIdleAsync();

        // 串行：A 完全结束后 B 才开始
        Assert.Equal(new[] { "A-start", "A-end", "B-start", "B-end" }, order);
        Assert.False(queue.IsRunning);
    }

    [Fact]
    public async Task Enqueue_WhileRunning_AppendsToQueue()
    {
        var queue = new ActionQueue();
        var order = new List<string>();

        queue.Enqueue(async () =>
        {
            order.Add("A");
            await Task.Delay(20);
        });

        // A 运行中排入 B，B 应等 A 完成
        queue.Enqueue(async () =>
        {
            order.Add("B");
            await Task.Delay(5);
        });

        await queue.WaitUntilIdleAsync();

        Assert.Equal(new[] { "A", "B" }, order);
    }

    [Fact]
    public async Task Enqueue_AfterIdle_RunsImmediately()
    {
        var queue = new ActionQueue();
        var first = true;

        queue.Enqueue(async () => { await Task.Delay(10); first = false; });
        await queue.WaitUntilIdleAsync();

        var order = new List<string>();
        queue.Enqueue(() => { order.Add("C"); return Task.CompletedTask; });
        await queue.WaitUntilIdleAsync();

        Assert.Equal(new[] { "C" }, order);
        Assert.False(first);
    }

    [Fact]
    public async Task Clear_RemovesPendingSteps()
    {
        var queue = new ActionQueue();
        var executed = new List<string>();

        queue.Enqueue(async () => { executed.Add("A"); await Task.Delay(50); });
        // A 运行中排入 B、C，然后清空（B、C 应被丢弃）
        queue.Enqueue(async () => { executed.Add("B"); await Task.Delay(1); });
        queue.Enqueue(async () => { executed.Add("C"); await Task.Delay(1); });
        queue.Clear();

        await queue.WaitUntilIdleAsync();

        Assert.Equal(new[] { "A" }, executed);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task Completed_State_ReflectsIdle()
    {
        var queue = new ActionQueue();
        Assert.True(queue.IsEmpty);

        queue.Enqueue(() => Task.CompletedTask);
        await queue.WaitUntilIdleAsync();

        Assert.True(queue.IsEmpty);
        Assert.False(queue.IsRunning);
        Assert.Equal(0, queue.PendingCount);
    }
}
