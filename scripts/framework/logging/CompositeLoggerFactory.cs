using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace BreakOut.scripts.framework.logging;

/// <summary>
///     组合日志工厂，为每个日志器创建共享一组 Appender 的组合日志器。
/// </summary>
internal sealed class CompositeLoggerFactory : ILoggerFactory
{
    private readonly ILogAppender[] _appenders;

    /// <summary>
    ///     创建组合日志工厂。
    /// </summary>
    /// <param name="appenders">所有日志器共享的输出器集合。</param>
    public CompositeLoggerFactory(ILogAppender[] appenders)
    {
        _appenders = appenders;
    }

    /// <summary>
    ///     获取或创建指定名称的组合日志器。
    /// </summary>
    /// <param name="name">日志器名称。</param>
    /// <param name="minLevel">最小日志级别。</param>
    /// <returns>组合日志器实例。</returns>
    public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
    {
        return new CompositeLogger(name, minLevel, _appenders);
    }
}
