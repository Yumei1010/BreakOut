namespace GFrameworkTemplate.scripts.framework.logging;

/// <summary>
///     会话日志文件信息，描述一次运行产生的日志文件位置。
/// </summary>
public sealed record SessionLogFileInfo(
    string FullPath,
    string DirectoryPath,
    string FileName);
