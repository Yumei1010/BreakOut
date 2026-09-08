using System;
using System.IO;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Logging.Formatters;
using BreakOut.scripts.framework.logging;

namespace BreakOut.Tests;

/// <summary>
///     会话日志组件测试：文件名生成与 JSON 落盘验证。
/// </summary>
public class SessionLoggingTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    ///     初始化测试：创建临时日志目录。
    /// </summary>
    public SessionLoggingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gfw_template_logging_" + Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Create_固定时间戳_生成带时间戳文件名()
    {
        var timestamp = new DateTime(2026, 9, 7, 15, 30, 0);

        var info = SessionLogFileHelper.Create(_tempDir, timestamp);

        Assert.Equal("20260907_153000.log", info.FileName);
        Assert.Equal(_tempDir, info.DirectoryPath);
        Assert.Equal(Path.Combine(_tempDir, "20260907_153000.log"), info.FullPath);
    }

    [Fact]
    public void Create_自动创建目录()
    {
        var nested = Path.Combine(_tempDir, "nested", "logs");

        var info = SessionLogFileHelper.Create(nested, DateTime.Now);

        Assert.True(Directory.Exists(nested));
        Assert.True(File.Exists(info.FullPath) == false, "仅创建目录，文件由 FileAppender 打开时创建");
    }

    [Fact]
    public void CreateNow_使用当前时间戳()
    {
        var before = DateTime.Now;
        var info = SessionLogFileHelper.CreateNow(_tempDir);
        var after = DateTime.Now;

        var ts = DateTime.ParseExact(info.FileName[..^4], "yyyyMMdd_HHmmss", null);
        Assert.InRange(ts, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Provider_写入Json格式日志行()
    {
        var filePath = Path.Combine(_tempDir, "session_test.log");
        string[] lines;
        using (var provider = new SessionFileLoggerFactoryProvider(new FileAppender(filePath, new JsonLogFormatter())))
        {
            var logger = provider.CreateLogger("Test.Category");

            logger.Info("你好 {0}", "模板");
            provider.Flush();
        }

        lines = File.ReadAllLines(filePath);
        Assert.Single(lines);

        using var doc = System.Text.Json.JsonDocument.Parse(lines[0]);
        var root = doc.RootElement;
        Assert.Equal("INFO", root.GetProperty("level").GetString());
        Assert.Equal("Test.Category", root.GetProperty("logger").GetString());
        Assert.Equal("你好 模板", root.GetProperty("message").GetString());
    }

    [Fact]
    public void Provider_结构化属性与异常写入Json()
    {
        var filePath = Path.Combine(_tempDir, "session_structured.log");
        string json;
        using (var provider = new SessionFileLoggerFactoryProvider(new FileAppender(filePath, new JsonLogFormatter())))
        {
            var logger = (GFramework.Core.Abstractions.Logging.IStructuredLogger)provider.CreateLogger("Test.Structured");

            logger.Log(
                GFramework.Core.Abstractions.Logging.LogLevel.Error,
                "计算失败",
                new InvalidOperationException("boom"),
                ("scope", "deck"));

            provider.Flush();
        }

        json = File.ReadAllText(filePath);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("ERROR", root.GetProperty("level").GetString());
        Assert.Equal("deck", root.GetProperty("properties").GetProperty("scope").GetString());
        var exception = root.GetProperty("exception");
        Assert.Contains("InvalidOperationException", exception.GetProperty("type").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_MinLevel过滤低级别日志()
    {
        var filePath = Path.Combine(_tempDir, "session_filter.log");
        string json;
        using (var provider = new SessionFileLoggerFactoryProvider(new FileAppender(filePath, new JsonLogFormatter()))
        {
            MinLevel = GFramework.Core.Abstractions.Logging.LogLevel.Warning
        })
        {
            var logger = provider.CreateLogger("Test.Filter");

            logger.Info("不应出现");
            logger.Warn("应当出现");

            provider.Flush();
        }

        json = File.ReadAllText(filePath);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("应当出现", doc.RootElement.GetProperty("message").GetString());
    }
}
