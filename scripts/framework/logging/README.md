# 会话文件日志（framework/logging）

把 GFramework 的日志输出从"只进 Godot 控制台"扩展为**控制台 + 会话文件双路输出**，日志以 JSON 结构化持久化，方便开发人员回看与分析错误。

> 属于"对 GFramework 的自研扩展"（framework 层）。GFramework Core 自带 `FileAppender` / `JsonLogFormatter` / `CompositeLogger`，本组件只做接线与 Godot 适配，不重复造轮子。

## 能力

- **会话切文件**：每次运行产生 `user://logs/YYYYMMDD_HHmmss.log`，互不覆盖
- **JSON 结构化**：每行一个 JSON 对象，含时间戳/级别/logger/消息/结构化属性/异常（type + message + stackTrace）
- **双路输出**：Godot 控制台保持原有彩色模板，文件端独立 JSON
- **级别过滤**：沿用 GFramework `MinLevel` 语义（`>=` 生效）
- **轻量导出**：启动打印会话日志完整路径 + 提供打开日志目录入口

## 文件构成

| 文件 | 职责 | 可单测 |
|---|---|---|
| `SessionFileLoggerFactoryProvider.cs` | `ILoggerFactoryProvider`：组合 Godot + 文件双 appender | ✅（注入纯文件 appender） |
| `CompositeLoggerFactory.cs` | 内部工厂：共享 appender 集合创建 `CompositeLogger` | — |
| `SessionLogFileInfo.cs` | 会话文件信息（record） | — |
| `SessionLogFileHelper.cs` | 生成会话文件名（`yyyyMMdd_HHmmss`）+ 建目录 | ✅ |
| `LogOpenHelper.cs` | 打印路径 + 打开目录（Godot 薄壳） | — |

## 接线（GameEntryPoint）

`global/GameEntryPoint.cs` 中把默认 provider 替换：

```csharp
// 替换前
LoggerFactoryProvider = new GodotLoggerFactoryProvider { MinLevel = LogLevel.Debug }

// 替换后（会话文件路径需先经 GlobalizePath 转真实路径）
var logDirectory = ProjectSettings.GlobalizePath("user://logs/");
SessionLogFile = SessionLogFileHelper.CreateNow(logDirectory);
_logProvider = new SessionFileLoggerFactoryProvider(SessionLogFile.FullPath)
{
    MinLevel = LogLevel.Debug
};
LoggerFactoryProvider = _logProvider;
```

启动即打印会话日志路径；`_ExitTree` 中 `Flush()` 确保尾部日志落盘。

### 打开日志目录

```csharp
GameEntryPoint.OpenSessionLogDirectory(); // 系统文件管理器打开 user://logs/
```

## 日志行示例（JSON）

> 注：文件首行含 UTF-8 BOM（框架 `FileAppender` 用 `Encoding.UTF8` 所致，追加不重复）——记事本/Excel 打开友好，接受。

```json
{"timestamp":"2026-09-08T02:22:40.8061506Z","level":"INFO","logger":"BreakOut.global.GameEntryPoint","message":"框架入口点就绪."}
{"timestamp":"2026-09-08T02:22:41.1000000Z","level":"ERROR","logger":"X","message":"计算失败","properties":{"scope":"deck"},"exception":{"type":"System.InvalidOperationException","message":"boom","stackTrace":"..."}}
```

## 结构化属性调用注意

`ILogger` 接口**没有**结构化属性重载（会绑定到 `params object[]` 把属性当格式参数丢弃）。带属性/异常的结构化日志需用 `IStructuredLogger` 类型调用：

```csharp
IStructuredLogger log = this.GetLogger("My.Category"); // 或强类型持有
log.Log(LogLevel.Error, "计算失败", exception, ("scope", "deck"));
```

## 测试

`tests/.../SessionLoggingTests.cs` 覆盖：文件名生成 / 目录创建 / 当前时间戳 / JSON 落盘 / 结构化属性+异常 / MinLevel 过滤。测试通过 internal 构造注入纯文件 appender，不依赖 Godot 运行时。

## 后续（v2 候选）

- 游戏内导出 UI（选目录拷贝日志）
- 崩溃兜底：`AppDomain.UnhandledException` 自动打 Fatal 日志
- 日志保留策略（如只留最近 N 个会话文件）
