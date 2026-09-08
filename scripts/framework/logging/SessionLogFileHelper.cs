using System;
using System.IO;

namespace GFrameworkTemplate.scripts.framework.logging;

/// <summary>
///     会话日志文件创建辅助类，负责生成按会话切分的日志文件路径。
/// </summary>
/// <remarks>
///     文件名格式：<c>yyyyMMdd_HHmmss.log</c>（如 <c>20260907_153000.log</c>）。
///     本类只做路径计算与目录创建，不依赖 Godot API，可独立单测。
/// </remarks>
public static class SessionLogFileHelper
{
    private const string LogFileExtension = ".log";

    /// <summary>
    ///     创建本次运行使用的会话日志文件信息。
    /// </summary>
    /// <param name="logDirectoryPath">日志目录路径（真实文件系统路径，非 <c>user://</c>）。</param>
    /// <param name="timestamp">会话时间戳，用于生成文件名。</param>
    /// <returns>会话日志文件信息。</returns>
    public static SessionLogFileInfo Create(string logDirectoryPath, DateTime timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectoryPath);

        Directory.CreateDirectory(logDirectoryPath);

        var fileName = timestamp.ToString("yyyyMMdd_HHmmss") + LogFileExtension;
        var fullPath = Path.Combine(logDirectoryPath, fileName);

        return new SessionLogFileInfo(fullPath, logDirectoryPath, fileName);
    }

    /// <summary>
    ///     使用当前时间创建会话日志文件信息。
    /// </summary>
    /// <param name="logDirectoryPath">日志目录路径（真实文件系统路径）。</param>
    /// <returns>会话日志文件信息。</returns>
    public static SessionLogFileInfo CreateNow(string logDirectoryPath)
    {
        return Create(logDirectoryPath, DateTime.Now);
    }
}
