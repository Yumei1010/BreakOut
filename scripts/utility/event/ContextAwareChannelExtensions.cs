using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Extensions;
using BreakOut.scripts.constants;

namespace BreakOut.scripts.utility.@event;

/// <summary>
///     频段事件总线扩展方法：在任意 <c>IContextAware</c> 节点中直接使用频段事件。
///     升级到 0.7.1 后，架构的 <c>IEventBus</c> 即 <see cref="ChannelEventBus"/>（通过 <c>GameArchitecture.Configurator</c> 注入），
///     因此这里直接从服务容器获取。
/// </summary>
/// <example>
///     <code>
///     // 在任意 [ContextAware] 节点中：
///     this.RegisterEvent&lt;PlayerDiedEvent&gt;(ChannelConstants.Gameplay, e =&gt; { ... });
///     this.SendEvent(ChannelConstants.Gameplay, new PlayerDiedEvent { PlayerId = 1 });
///     this.SendEvent&lt;GameStartedEvent&gt;(ChannelConstants.Gameplay); // 无数据事件
///     </code>
/// </example>
public static class ContextAwareChannelExtensions
{
    /// <summary>
    ///     获取架构的频段事件总线（由 <c>GameArchitecture.Configurator</c> 注入的 ChannelEventBus）。
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>频段事件总线实例</returns>
    private static ChannelEventBus GetChannelBus(this GFramework.Core.Abstractions.Rule.IContextAware contextAware)
    {
        return contextAware.GetService<ChannelEventBus>()
            ?? throw new InvalidOperationException("ChannelEventBus not registered in architecture");
    }

    /// <summary>
    ///     订阅指定频段上的事件。
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="channel">频段名称（见 <see cref="ChannelConstants"/>）</param>
    /// <param name="handler">事件处理回调</param>
    /// <returns>取消订阅句柄</returns>
    public static IUnRegister RegisterEvent<TEvent>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware, string channel, Action<TEvent> handler)
    {
        return contextAware.GetChannelBus().RegisterOnChannel(channel, handler);
    }

    /// <summary>
    ///     向指定频段发送事件（带数据）。
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="channel">频段名称（见 <see cref="ChannelConstants"/>）</param>
    /// <param name="eventData">事件数据</param>
    public static void SendEvent<TEvent>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware, string channel, TEvent eventData)
    {
        contextAware.GetChannelBus().SendOnChannel(channel, eventData);
    }

    /// <summary>
    ///     向指定频段发送事件（无数据标记事件）。
    /// </summary>
    /// <typeparam name="TEvent">事件类型（需有公共无参构造函数）</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <param name="channel">频段名称（见 <see cref="ChannelConstants"/>）</param>
    public static void SendEvent<TEvent>(this GFramework.Core.Abstractions.Rule.IContextAware contextAware, string channel) where TEvent : new()
    {
        contextAware.GetChannelBus().SendOnChannel<TEvent>(channel);
    }
}
