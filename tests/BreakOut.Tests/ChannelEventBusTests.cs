using BreakOut.scripts.constants;
using BreakOut.scripts.utility.@event;

namespace BreakOut.Tests;

/// <summary>
///     频段事件总线测试。
///     验证：不同频段同名事件互不干扰、同频段多订阅者、取消订阅。
/// </summary>
public class ChannelEventBusTests
{
    /// <summary>
    ///     测试事件（带数据）。
    /// </summary>
    private sealed class TestEvent
    {
        public required string Value { get; init; }
    }

    /// <summary>
    ///     测试标记事件（无数据）。
    /// </summary>
    private sealed class MarkerEvent;

    [Fact]
    public void Send_ToChannel_OnlyNotifiesThatChannelSubscribers()
    {
        var bus = new ChannelEventBus();
        var gameplayReceived = new List<string>();
        var uiReceived = new List<string>();

        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Gameplay, e => gameplayReceived.Add(e.Value));
        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Ui, e => uiReceived.Add(e.Value));

        bus.SendOnChannel(ChannelConstants.Gameplay, new TestEvent { Value = "player-died" });

        Assert.Equal(new[] { "player-died" }, gameplayReceived);
        Assert.Empty(uiReceived);
    }

    [Fact]
    public void Send_ToMultipleChannels_NotifiesRespectiveSubscribers()
    {
        var bus = new ChannelEventBus();
        var gameplayReceived = new List<string>();
        var uiReceived = new List<string>();

        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Gameplay, e => gameplayReceived.Add(e.Value));
        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Ui, e => uiReceived.Add(e.Value));

        bus.SendOnChannel(ChannelConstants.Gameplay, new TestEvent { Value = "a" });
        bus.SendOnChannel(ChannelConstants.Ui, new TestEvent { Value = "b" });

        Assert.Equal(new[] { "a" }, gameplayReceived);
        Assert.Equal(new[] { "b" }, uiReceived);
    }

    [Fact]
    public void SameChannel_MultipleSubscribers_AllReceive()
    {
        var bus = new ChannelEventBus();
        var received1 = new List<string>();
        var received2 = new List<string>();

        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Gameplay, e => received1.Add(e.Value));
        bus.RegisterOnChannel<TestEvent>(ChannelConstants.Gameplay, e => received2.Add(e.Value));

        bus.SendOnChannel(ChannelConstants.Gameplay, new TestEvent { Value = "x" });

        Assert.Equal(new[] { "x" }, received1);
        Assert.Equal(new[] { "x" }, received2);
    }

    [Fact]
    public void UnRegister_StopsReceiving()
    {
        var bus = new ChannelEventBus();
        var received = new List<string>();

        var unReg = bus.RegisterOnChannel<TestEvent>(ChannelConstants.Gameplay, e => received.Add(e.Value));
        bus.SendOnChannel(ChannelConstants.Gameplay, new TestEvent { Value = "before" });

        unReg.UnRegister();
        bus.SendOnChannel(ChannelConstants.Gameplay, new TestEvent { Value = "after" });

        Assert.Equal(new[] { "before" }, received);
    }

    [Fact]
    public void MarkerEvent_NoData_SendsToChannel()
    {
        var bus = new ChannelEventBus();
        var gameplayFired = false;
        var uiFired = false;

        bus.RegisterOnChannel<MarkerEvent>(ChannelConstants.Gameplay, _ => gameplayFired = true);
        bus.RegisterOnChannel<MarkerEvent>(ChannelConstants.Ui, _ => uiFired = true);

        bus.SendOnChannel<MarkerEvent>(ChannelConstants.Gameplay);

        Assert.True(gameplayFired);
        Assert.False(uiFired);
    }

    [Fact]
    public void CustomChannel_Works()
    {
        var bus = new ChannelEventBus();
        var received = new List<string>();

        bus.RegisterOnChannel<TestEvent>("MyCustomChannel", e => received.Add(e.Value));
        bus.SendOnChannel("MyCustomChannel", new TestEvent { Value = "custom" });

        Assert.Equal(new[] { "custom" }, received);
    }
}
