using GFramework.Core.Abstractions.Controller;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.SourceGenerators.Abstractions;
using GFramework.Godot.SourceGenerators.Abstractions.UI;
using Godot;
using BreakOut.scripts.core.ui;
using BreakOut.scripts.enums.ui;

namespace BreakOut.scripts.menu;

/// <summary>
///     模板页面——语法糖版 UI 页面示例（partial class 模式）
///     <see cref="AutoUiPageAttribute"/> 自动生成 <c>UiKeyStr</c> + <c>GetPage()</c> + 缓存字段样板
/// </summary>
[Log]
[ContextAware]
[AutoUiPage(nameof(UiKey.TemplatePage), nameof(UiLayer.Page))]
public partial class TemplatePage : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
    /// <summary>
    ///     Godot 节点就绪回调：节点注入 → 信号绑定 → 异步初始化 → 事件注册
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();      // [GetNode] 字段注入（生成器提供）
        __BindNodeSignals_Generated();     // [BindNodeSignal] 信号绑定（生成器提供）
        _ = ReadyAsync();
        RegisterEvents();
    }

    /// <summary>
    ///     节点退出场景树：解绑 [BindNodeSignal] 信号（防泄漏）
    /// </summary>
    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }
}
