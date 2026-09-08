# 项目约束规范

本文档定义基于本框架模板的项目的编码规范、架构约束和命名约定。所有贡献者必须遵守。

---

## 1. 命名空间规范

### 规则

命名空间与目录层次一一对应，使用文件范围声明（`namespace X.Y.Z;`，带分号无大括号）。

```
根命名空间: BreakOut

global/ 目录                    → BreakOut.global;
scripts/component/<name>/       → BreakOut.scripts.component.<name>;
scripts/entities/<name>/        → BreakOut.scripts.entities.<name>;
scripts/system/<name>/          → BreakOut.scripts.system.<name>;
scripts/enums/<domain>/         → BreakOut.scripts.enums.<domain>;
scripts/menu/<name>/            → BreakOut.scripts.menu.<name>;
scripts/core/<dir>/             → BreakOut.scripts.core.<dir>;
scripts/cqrs/<domain>/command/  → BreakOut.scripts.cqrs.<domain>.command;
scripts/cqrs/<domain>/command/input/ → BreakOut.scripts.cqrs.<domain>.command.input;
scripts/cqrs/<domain>/event/    → BreakOut.scripts.cqrs.<domain>.@event;
```

### 注意事项

- C# 关键字 `event` 在命名空间中转义为 `@event`
- `global/` 使用 `BreakOut.global`（不含 `scripts.` 前缀）
- 禁止使用传统的花括号命名空间 `namespace X { }` 语法

---

## 2. 文件与目录结构

### 目录分类

| 目录 | 用途 | 示例 |
|---|---|---|
| `scripts/component/` | 可复用组件（含接口和实现） | HFSM、行为树 |
| `scripts/entities/` | 领域实体与核心组件 | 按业务域自定义 |
| `scripts/system/` | GFramework ISystem 实现 | 按业务域自定义 |
| `scripts/menu/` | UI 页面（被 UiRouter 管理） | 按业务域自定义 |
| `scripts/cqrs/` | CQRS 命令、事件、命令输入 | 见第 4 节 |
| `scripts/enums/` | 枚举定义（按域分子目录） | UiKey、SceneKey、TextureKey |
| `scripts/model/` | 领域模型（纯数据结构） | 按业务域自定义 |
| `scripts/core/` | 架构核心（状态机、路由、UI 工厂） | GameArchitecture、UiRouter |
| `scripts/module/` | GFramework 模块安装 | ModelModule、SystemModule |
| `scripts/constants/` | 全局常量 | GameConstants、UiLayers |
| `scripts/data/` | 可持久化数据类与提供者 | SettingDataLocationProvider |
| `scripts/utility/` | 通用工具，按域分子目录 | GameUtil、event/、registry/ |
| `global/` | Godot 自动加载单例 | GameEntryPoint、UiRoot、SceneRoot |
| `tests/` | xUnit 单元测试 | 按模块自定义 |

### 目录命名规范

- **文件夹名全部小写**，单词间用下划线分隔（snake_case）
- 例：`annotation_tool`、`mode_bar`、`time_bar`
- 禁止驼峰命名（如 ~~`annotationTool`~~、~~`TimeBar`~~）

### Godot .uid 文件

每个 `.cs` 文件自动生成一个 `.cs.uid` 文件（Godot 资源 UID），必须纳入版本控制。

---

## 3. Partial Class 模式

### 约定

Godot 节点类使用 partial class 拆分，每种职责一个文件：

| 后缀 | 职责 | 必须 |
|---|---|---|
| `.cs` | 核心：`_Ready()`、公开方法、生命周期 | 是 |
| `.Dependencies.cs` | Godot 节点引用（`GetNode<T>("%Name")`）、`ReadyAsync()` | 推荐 |
| `.Properties.cs` | 字段、属性、`UiKeyStr` | 推荐 |
| `.Events.cs` | CQRS 事件订阅（`RegisterEvent()`） | 推荐 |
| `.Signals.cs` | Godot 信号连接（`ConnectSignal()`） | 按需 |

### 示例

```
MyPage.cs
MyPage.Dependencies.cs
MyPage.Events.cs
MyPage.Properties.cs
```

### 命名规则

- 文件名格式：`{ClassName}.{Suffix}.cs`
- 所有 partial 文件声明为 `public partial class {ClassName}`（不加修饰符）
- `_Ready()` 中调用顺序：`ReadyAsync()` → `ConnectSignal()` → `RegisterEvent()`

---

## 4. CQRS 事件与命令规范

### 目录结构

```
scripts/cqrs/<domain>/
    command/                          # 命令实现
        <Action>Command.cs
        input/                        # 命令输入（纯数据载体）
            <Action>CommandInput.cs
    event/                            # 事件定义
        <Name>Event.cs
```

### 事件规范

**所有事件必须是 `public sealed class`。**

**数据事件**（携带属性）— 使用完整类体：
```csharp
namespace BreakOut.scripts.cqrs.<domain>.@event;

public sealed class SomeEvent
{
    public required string Prop1 { get; init; }
    public required int Prop2 { get; init; }
}
```

**标记事件**（无数据）— 使用文件范围类型声明：
```csharp
namespace BreakOut.scripts.cqrs.<domain>.@event;

/// <summary>
///     某某事件类，用于表示某某事件
/// </summary>
public sealed class SomeEvent;
```

**事件属性强制约束：**
- 所有属性使用 `{ get; init; }`（不可变）
- 所有属性使用 `required` 修饰符
- 禁止使用 `{ get; set; }`（事件不可变）

### 命令规范

**所有命令必须是 `public sealed class`。**

**带输入的命令**（异步）— 使用主构造函数：
```csharp
namespace BreakOut.scripts.cqrs.<domain>.command;

public sealed class SomeCommand(SomeCommandInput input)
    : AbstractAsyncCommand<SomeCommandInput>(input)
{
    protected override async Task OnExecuteAsync(SomeCommandInput input)
    {
        // 业务逻辑
    }
}
```

**无输入的命令**（同步）：
```csharp
public sealed class SimpleCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        // 业务逻辑
    }
}
```

**带属性的命令**（同步，无输入类）：
```csharp
public sealed class SomeCommand : AbstractCommand
{
    public required Guid TargetId { get; set; }
    public required float Value { get; set; }

    protected override void OnExecute()
    {
        // 使用 this.TargetId、this.Value
    }
}
```

**命令属性强制约束：**
- 所有属性使用 `{ get; set; }`（可写，命令执行前设置）
- 所有属性使用 `required` 修饰符
- 禁止使用 `{ get; init; }`（命令需要可变性）

### 命令输入规范

**所有命令输入必须是 `public sealed class`，实现 `ICommandInput`：**

```csharp
namespace BreakOut.scripts.cqrs.<domain>.command.input;

public sealed class SomeCommandInput : ICommandInput
{
    public float Value { get; set; }
}
```

- 命令输入使用 `{ get; set; }`（在命令执行前设置）
- 禁止使用 `struct` 作为命令输入类型（统一为 `class`）

---

## 5. 接口规范

### 命名

- 接口以 `I` 为前缀：`ICalculator`、`IPoker`、`IDeck`
- 接口文件与主要实现放在同一目录
- 一个接口一个文件

### 分类

| 类别 | 接口特征 | 示例 |
|---|---|---|
| **组件契约** | 定义在 `component/`，实现也在 `component/` | `ICalculator` → `Calculator` |
| **实体契约** | 定义在 `entities/`，实现也在 `entities/` | `IPoker` → `Poker` |
| **跨层抽象** | 定义在 `component/`，实现在 `entities/` | `IState` → `PokerState` |
| **UI 页面** | **不需要** `I*` 接口，页面由 UiRouter 管理 | MainMenu、CalculateMenu |

### 规则

- UI 页面（`scripts/menu/` 下的类）**不需要**提取 `I*` 接口
- 页面通过 `GetNode<IComponent>("%NodeName")` 消费组件，但不被其他组件消费
- `ISimpleUiPage` 是页面生命周期契约，由 GFramework 的 UiRouter 调用

---

## 6. XML 文档注释规范

### 适用层级

| 层级 | 是否需要 XML 注释 |
|---|---|
| **接口（`I*.cs`）** | **必须** — 完整的 `<summary>`、`<param>`、`<returns>`、`<remarks>` |
| **抽象基类** | **必须** — 说明模板方法和可重写点 |
| **组件公开方法** | **推荐** — `<summary>` 说明非显而易见的逻辑 |
| **CQRS 事件/命令** | **必须** — 类级 `<summary>` |
| **命令输入** | **必须** — 类级 `<summary>`，属性级可选 |
| **枚举** | **推荐** — 类级 `<summary>` |
| **具体子类（Mode/State）** | **可选** — 良好的命名足以自解释 |
| **partial 内部方法** | **按需** — 仅在逻辑非显而易见时添加 |

### 格式

```csharp
/// <summary>
///     中文描述，一句或两句。
/// </summary>
/// <param name="paramName">参数中文描述 <see cref="TypeName"/></param>
/// <returns>返回值中文描述</returns>
/// <remarks>
///     <para>额外说明段落。</para>
///     <code>
///         // 使用示例
///     </code>
/// </remarks>
```

### 规则

- 注释语言：中文
- `<see cref=""/>` 用于引用类型；`<c>` 用于内联代码
- 缩进：`///` 后 5 空格 + 内容
- 每个 `<param>` 使用 `name` 属性
- 事件属性的 `<see cref=""/>` 放在描述末尾

---

## 7. 类声明修饰符规范

| 类别 | 修饰符 | 继承自 |
|---|---|---|
| **事件（有数据）** | `public sealed class` | — |
| **事件（标记）** | `public sealed class` (分号声明) | — |
| **命令** | `public sealed class` | `AbstractCommand(Async)` 或 `AbstractCommand(Async)<T>` |
| **命令输入** | `public sealed class` | `ICommandInput` |
| **Godot 组件/实体/页面** | `public partial class` | Godot `Node`/`Control`/`Button` 等 + 接口 |
| **架构类** | `public sealed class` | `AbstractArchitecture` |
| **工具类** | `public class` | `AbstractContextUtility` |

### 关键规则

- 事件和命令**必须** `sealed`
- Godot 节点类使用 `partial`（因 Godot 源代码生成器要求）
- Godot 节点类**不能** `sealed`（需要 Godot 生成派生类）

---

## 8. Godot 节点注入规范

### 模式

所有 Godot 场景节点引用集中在 `*.Dependencies.cs` 文件中，使用表达式体属性：

```csharp
// 组件类型 — 通过接口注入
private ICalculator Calculator => GetNode<ICalculator>("%Calculator");

// Godot 原生类型 — 通过具体类型注入
private Button CheckButton => GetNode<Button>("%CheckButton");
private TextureRect ShadowRect => GetNode<TextureRect>("%ShadowRect");
```

### 规则

- 全部使用 `%` 唯一名称语法（Godot 4.x unique name）
- 属性访问级别：`private`（不对外暴露）
- 属性类型选择：
  - 若目标实现了项目接口 → 使用接口类型（`ICalculator`）
  - 否则 → 使用 Godot 具体类型（`Button`、`Label`）
- 属性声明位置：**仅**在 `*.Dependencies.cs` 中

---

## 9. GFramework 框架属性

### [Log] 和 [ContextAware]

实现 `IController` 的 Godot 节点类必须同时标注：

```csharp
[Log]
[ContextAware]
public partial class Calculator : Node, ICalculator
```

- `[Log]` 来自 `GFramework.Core.SourceGenerators.Abstractions.Logging` — 自动生成静态 `Log` 属性
- `[ContextAware]` 来自 `GFramework.Core.SourceGenerators.Abstractions.Rule` — 自动注入架构上下文
- **两者成对出现**，`[Log]` 在前，`[ContextAware]` 在后

### CQRS 通信 API

```csharp
// 发送事件
this.SendEvent(new SomeEvent { Prop = value });

// 注册事件监听（需在 RegisterEvent() 中调用）
this.RegisterEvent<SomeEvent>(e => { /* 处理 */ })
    .UnRegisterWhenNodeExitTree(this);

// 发送命令
this.SendCommand(new SomeCommand(input));
```

---

## 10. 全局引用

### GlobalUsings.cs

位于 `scripts/GlobalUsings.cs`，仅包含以下 5 个全局 using：

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using LanguageExt;
```

### 规则

- 不得向 `GlobalUsings.cs` 添加 Godot 或 GFramework 命名空间
- 每个文件显式导入所需的 Godot/GFramework/项目命名空间
- using 导入按以下顺序分组（组间空行）：
  1. GFramework 相关
  2. Godot 相关
  3. 项目内部

### 示例

```csharp
using GFramework.Core.extensions;
using GFramework.Godot.extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using Godot;
using BreakOut.scripts.entities.my_entity;
using BreakOut.scripts.cqrs.my_domain.@event;
```

---

## 11. 提交规范

### 提交信息格式

```
<type>(<scope>): <简短中文描述>
```

### Type 类型

| Type | 含义 |
|---|---|
| `feat` | 新功能 |
| `refactor` | 重构（无功能变更） |
| `fix` | Bug 修复 |
| `test` | 测试相关 |
| `docs` | 文档 |
| `chore` | 构建/工具/杂项 |

### 规则

- 每条提交必须是**逻辑独立的原子操作**
- 不同组件/职责的修改**必须拆分提交**
- 描述使用**中文**
- 提交信息结尾追加 `Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>`
- 禁止使用 `--no-verify` 跳过钩子

### 示例

```
feat(MyComponent): 添加新组件，通过事件驱动控制

refactor(MyMenu): 数据驱动方式重构按钮连接

fix(MyModel): 修复边界条件校验问题
```

---

## 附录 A：检查清单

新增/修改文件时的自查项：

- [ ] 命名空间与目录路径一致
- [ ] 文件名符合 `{ClassName}.{Suffix}.cs` 格式
- [ ] 事件/命令标记为 `sealed`
- [ ] 事件属性使用 `{ get; init; }` 且带 `required`
- [ ] 命令属性使用 `{ get; set; }` 且带 `required`
- [ ] 命令输入为 `sealed class`（非 `struct`），属性使用 `{ get; set; }`
- [ ] 接口有完整的 XML 文档注释
- [ ] 公开方法有 `<summary>` 注释
- [ ] Godot 节点引用使用 `%` 唯一名称
- [ ] `[Log]` 和 `[ContextAware]` 成对出现
- [ ] `dotnet build` 0 错误
- [ ] `dotnet test` 全部通过

## 附录 B：禁止事项

- ❌ 传统花括号命名空间 `namespace X { }`
- ❌ 事件的 `{ get; set; }` 可变属性
- ❌ 命令的 `{ get; init; }` 不可变属性
- ❌ 命令输入使用 `struct`
- ❌ 事件/命令不加 `sealed`
- ❌ 驼峰命名的目录名（全部使用 snake_case）
- ❌ 在 `GlobalUsings.cs` 中添加 Godot/GFramework 引用
- ❌ UI 页面提取 `I*` 接口
- ❌ 在 `_Ready()` 中直接编写业务逻辑（应委托给 `ReadyAsync`/`ConnectSignal`/`RegisterEvent`）
- ❌ 跳过 Git 钩子（`--no-verify`）
- ❌ 混合多种职责的单一提交
