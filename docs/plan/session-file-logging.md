# 会话文件日志（Session File Logging）

> 状态：已实现（2026-09）| 依赖：GFramework 0.7.1

## 需求

日志数据需持久化保存导出，方便开发人员查看分析错误。

用户拍板的三项决策：
1. **按会话切文件**，文件名格式 `YYYYMMDD_HHmmss.log`
2. **文件内容用 Json**（结构化，可被工具分析）
3. **导出做轻量版**：日志路径打印 + 打开所在目录

## 调研结论（GFramework 0.7.1）

框架已内置 90% 能力，无需造轮子：

| 能力 | 框架位置 | 说明 |
|---|---|---|
| `FileAppender` | `GFramework.Core.Logging.Appenders` | 追加写文件，线程安全 |
| `RollingFileAppender` | 同上 | 按大小轮转 + 保留数量 |
| `AsyncLogAppender` | 同上 | 缓冲异步写 |
| `JsonLogFormatter` | `GFramework.Core.Logging.Formatters` | 结构化 JSON 输出 |
| `CompositeLogger` | `GFramework.Core.Logging` | 一 logger 多 appender |
| `CachedLoggerFactory` | `GFramework.Core.Logging` | logger 实例缓存 |
| `GodotLogAppender` | `GFramework.Godot.Logging` | Godot 控制台输出端（已 appender 化） |

框架作者归档（`ai-plan/public/archive/godot-logging-core-sink`）明确设计边界：
> Godot 输出可作为 Core appender 被自定义 factory / `CompositeLogger` 组合；
> 文件、JSON、滚动等仍由 Core logging 组件负责，Godot 包只提供宿主落点。

**模板差距**：`GameEntryPoint` 现用 `GodotLoggerFactoryProvider`（只连控制台），
未走 Core 多 appender 管线。需自研一个 provider 把控制台 + 会话文件组合起来。

## 设计

### 目标：会话日志 provider + 会话文件 + 轻量导出

```
scripts/framework/logging/
├── SessionFileLoggerFactoryProvider.cs   # ILoggerFactoryProvider：组合 Godot + 会话文件
├── SessionLogFileInfo.cs                 # 会话文件信息（只读数据：完整路径 + 目录 + 文件名）
├── SessionLogFileHelper.cs               # 创建会话文件（纯逻辑，可单测）
├── LogOpenHelper.cs                      # 打开日志目录（薄壳，_Process/OS 层）
└── README.md
```

### 接线点

`global/GameEntryPoint.cs` 现有代码：

```csharp
LoggerFactoryProvider = new GodotLoggerFactoryProvider
{
    MinLevel = LogLevel.Debug
}
```

改为：

```csharp
LoggerFactoryProvider = new SessionFileLoggerFactoryProvider
{
    MinLevel = LogLevel.Debug
}
```

Provider 内部持有共享 appender 数组：

```
[GodotLogAppender(控制台, 保持彩色模板), FileAppender(会话文件, Json)]
```

`CreateLogger(name)` → `CachedLoggerFactory` 包装 → `CompositeLogger(name, minLevel, appenders)`。
每个 logger 实例共享同一组 appender，文件只开一次（单 StreamWriter）。

### 会话文件创建

`SessionLogFileHelper.Create()`：
1. `ProjectSettings.GlobalizePath("user://logs/")` → 真实路径（FileAppender 用 System.IO，不认 `user://`）
2. `Directory.CreateDirectory(目录)`
3. 文件名 `DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log"`
4. 返回 `SessionLogFileInfo(完整路径, 目录, 文件名)`

### Json 内容格式

`JsonLogFormatter` 输出每行一个 JSON 对象：

```json
{"timestamp":"2026-09-07T10:30:00.0000000Z","level":"INFO","logger":"BreakOut.global.GameEntryPoint","message":"框架入口点就绪."}
```

- 带结构化属性时附加 `"properties":{...}`
- 异常（`Log(msg, exception)`）会走 `Exception` 字段，后续评估是否在 formatter 输出
- 注意：`CompositeLogger` 结构化属性由自定义 `Log(...properties)` 处理（见风险）

### 轻量导出（v1）

- 启动时打印：`GD.Print("[Log] 会话日志: <真实完整路径>")`（可点击跳转）
- 提供 `LogOpenHelper.OpenLogDirectory()`：`OS.ShellOpen(目录)` 打开文件管理器
- 暂不做游戏内导出 UI（v2 再议）

### 落层与沉淀

- 属于"对 GFramework 的自研扩展" → `scripts/framework/logging/`
- 与目录重构方案中的 `framework/`（event/registry/utility）同层
- 可单测部分：`SessionLogFileHelper`（用系统临时目录注入）→ 不依赖 Godot
- 完成后写教学文档 `docs/guides/logging.md` 说明 GFramework 日志架构 + 本组件用法

## 验收

- [x] `dotnet build` 通过（含 Godot 场景编译）
- [x] `dotnet test` 通过（新增 6 个单测）
- [x] 运行游戏：`user://logs/YYYYMMDD_HHmmss.log` 生成，内容为 JSON 行（headless 实测 129 行）
- [x] 控制台仍彩色输出（Debug 模板），不回归
- [x] 启动打印会话日志真实路径；F12（IsDev）打开日志目录

## 实现摘要

- `scripts/framework/logging/`：`SessionFileLoggerFactoryProvider`（组合 Godot + 文件双 appender）、`CompositeLoggerFactory`、`SessionLogFileInfo`、`SessionLogFileHelper`、`LogOpenHelper` + README
- `global/GameEntryPoint.cs`：接线 provider + 启动打印路径 + `_ExitTree` Flush + `_UnhandledInput` F12 打开目录
- 测试 6 个（`SessionLoggingTests.cs`）：文件名/目录/时间戳/JSON 落盘/结构化属性+异常/MinLevel 过滤
- 结构化属性调用须走 `IStructuredLogger`（`ILogger` 无该重载，会误绑 `params object[]`）
- 文件首行含 UTF-8 BOM（框架 `Encoding.UTF8` 所致，追加不重复）——Windows 工具打开友好，接受

## 风险与注意

1. **GodotLogAppender 内部构造**：需确认 public 构造可用（不依赖 Godot 配置源）
2. **`CompositeLogger` 结构化属性**：`Write(level, message, exception)` 不传 properties，
   需确认 CompositeLogger 是否重写 `Log(...properties)` 保留属性给 appender
3. **退出刷新**：退出时需 `Flush()` 保证尾部日志落盘（GameEntryPoint._ExitTree 或 AppDomain exit）
4. **LoggerProperties 与 Provider.MinLevel 的时序**：架构初始化时读取一次，确认热改不支持（当前可接受）

## 相关

- 前置调研记录见本项目会话（GFramework 源码阅读）
- 目录重构方案暂缓，本组件先放 `scripts/framework/logging/` 独立目录
