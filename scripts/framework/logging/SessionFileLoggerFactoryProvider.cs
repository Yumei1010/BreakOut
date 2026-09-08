using System;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Logging.Formatters;
using GFramework.Godot.Logging;

namespace BreakOut.scripts.framework.logging;

/// <summary>
///     会话文件日志工厂提供者：让每条日志同时输出到 Godot 控制台与本次会话的日志文件。
/// </summary>
/// <remarks>
///     组合了框架自带的 Godot 输出端与文件输出端（JSON 格式），走 Core 多 Appender 管线。
///     文件路径必须是真实文件系统路径（先经 <c>ProjectSettings.GlobalizePath</c> 转换，<c>FileAppender</c> 不认 <c>user://</c>）。
/// </remarks>
public sealed class SessionFileLoggerFactoryProvider : ILoggerFactoryProvider, IDisposable
{
    private readonly ILogAppender[] _appenders;
    private readonly CachedLoggerFactory _factory;

    /// <summary>
    ///     创建会话文件日志提供者：Godot 控制台 + JSON 会话文件双路输出。
    /// </summary>
    /// <param name="sessionFilePath">本次会话日志文件的真实路径（含文件名）。</param>
    public SessionFileLoggerFactoryProvider(string sessionFilePath)
        : this(
            new GodotLogAppender(),
            new FileAppender(sessionFilePath, new JsonLogFormatter()))
    {
        SessionFilePath = sessionFilePath;
    }

    /// <summary>
    ///     使用显式输出器集合创建提供者（供测试注入，避免依赖 Godot 输出端）。
    /// </summary>
    /// <param name="appenders">日志输出器集合。</param>
    internal SessionFileLoggerFactoryProvider(params ILogAppender[] appenders)
    {
        ArgumentNullException.ThrowIfNull(appenders);
        if (appenders.Length == 0)
        {
            throw new ArgumentException("At least one appender is required.", nameof(appenders));
        }

        _appenders = appenders;
        _factory = new CachedLoggerFactory(new CompositeLoggerFactory(appenders));
    }

    /// <summary>
    ///     获取或设置日志器的最小日志级别，低于此级别的日志将被忽略。
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    ///     获取本次会话日志文件的完整路径（公共构造创建时为非空）。
    /// </summary>
    public string? SessionFilePath { get; }

    /// <summary>
    ///     获取或创建指定名称的组合日志器实例（带缓存）。
    /// </summary>
    /// <param name="name">日志器名称。</param>
    /// <returns>组合日志器实例。</returns>
    public ILogger CreateLogger(string name)
    {
        return _factory.GetLogger(name, MinLevel);
    }

    /// <summary>
    ///     刷新所有输出器的缓冲区，确保日志已落盘。
    /// </summary>
    public void Flush()
    {
        foreach (var appender in _appenders)
        {
            appender.Flush();
        }
    }

    /// <summary>
    ///     释放所有输出器持有的资源。
    /// </summary>
    public void Dispose()
    {
        foreach (var appender in _appenders)
        {
            switch (appender)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}
