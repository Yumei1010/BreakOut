using GFramework.Core.Abstractions.Events;
using GFramework.Core.Events;

namespace BreakOut.scripts.utility.@event;

/// <summary>
///     频段事件总线实现：**继承框架原版 <see cref="EventBus"/>**，
///     在保留原版按类型分发能力的同时，增加"频段（Channel）"维度：
///     同名事件发到不同频段互不干扰，订阅者只收到自己订阅频段的事件。
/// </summary>
/// <remarks>
///     继承原版 EventBus 使其天然实现 <c>IEventBus</c>，可通过架构 <c>Configurator</c>
///     覆盖容器中的 IEventBus 注册，让原版 RegisterEvent / SendEvent 直接支持频段。
/// </remarks>
public class ChannelEventBus : EventBus
{
    private readonly Dictionary<string, EventBus> _channels = new(StringComparer.Ordinal);

    /// <summary>
    ///     订阅指定频段上的事件。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="channel">频段名称</param>
    /// <param name="handler">事件处理回调</param>
    /// <returns>取消订阅句柄</returns>
    public IUnRegister RegisterOnChannel<T>(string channel, Action<T> handler)
    {
        return GetChannel(channel).Register(handler);
    }

    /// <summary>
    ///     向指定频段发送事件（带数据）。
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="channel">频段名称</param>
    /// <param name="eventData">事件数据</param>
    public void SendOnChannel<T>(string channel, T eventData)
    {
        GetChannel(channel).Send(eventData);
    }

    /// <summary>
    ///     向指定频段发送事件（无数据标记事件）。
    /// </summary>
    /// <typeparam name="T">事件类型（需有公共无参构造函数）</typeparam>
    /// <param name="channel">频段名称</param>
    public void SendOnChannel<T>(string channel) where T : new()
    {
        GetChannel(channel).Send(new T());
    }

    /// <summary>
    ///     获取指定频段的 EventBus，不存在则创建。
    /// </summary>
    /// <param name="channel">频段名称</param>
    /// <returns>该频段对应的 EventBus</returns>
    private EventBus GetChannel(string channel)
    {
        if (!_channels.TryGetValue(channel, out var bus))
        {
            bus = new EventBus();
            _channels[channel] = bus;
        }

        return bus;
    }
}
