using Godot;

namespace GFrameworkTemplate.scripts.framework.logging;

/// <summary>
///     日志访问辅助类：负责打印会话日志路径与打开日志所在目录。
/// </summary>
public static class LogOpenHelper
{
    /// <summary>
    ///     在 Godot 输出面板打印会话日志文件的完整路径。
    /// </summary>
    /// <param name="filePath">会话日志文件的完整路径。</param>
    public static void PrintSessionLogPath(string filePath)
    {
        GD.Print($"[Log] 会话日志文件: {filePath}");
    }

    /// <summary>
    ///     打开日志文件所在目录（调用系统文件管理器）。
    /// </summary>
    /// <param name="directoryPath">日志目录路径（真实文件系统路径）。</param>
    /// <returns>打开成功返回 true；目录不存在或系统调用失败返回 false。</returns>
    public static bool OpenLogDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !DirAccess.DirExistsAbsolute(directoryPath))
        {
            GD.PrintErr($"[Log] 日志目录不存在: {directoryPath}");
            return false;
        }

        var error = OS.ShellOpen(directoryPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[Log] 打开日志目录失败: {error}");
            return false;
        }

        return true;
    }
}
