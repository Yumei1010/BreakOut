// meta-name: 简单UI页面控制器类模板
// meta-description: 负责管理UI页面场景的生命周期和架构关联（语法糖版：AutoUiPage + GetNode + BindNodeSignal）
using Godot;
using GFramework.Core.Abstractions.Controller;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.SourceGenerators.Abstractions;
using GFramework.Godot.SourceGenerators.Abstractions.UI;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.core.ui;
using BreakOut.scripts.enums.ui;
using BreakOut.global;

/// <summary>
///     _CLASS_ UI 页面——语法糖模板
///     [AutoUiPage] 自动生成 UiKeyStr + GetPage() + 缓存字段
///     节点引用 [GetNode]、信号绑定 [BindNodeSignal] 均为声明式字段/方法语法糖
/// </summary>
[Log]
[ContextAware]
[AutoUiPage(nameof(UiKey._CLASS_), nameof(UiLayer.Page))]
public partial class _CLASS_ : _BASE_, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
    // 节点字段注入（字段名 → %唯一名，场景需配置 unique_name_in_owner）
    // [GetNode] private Button _startButton = null!;

    // 信号语法糖：自动生成 __BindNodeSignals_Generated / __UnbindNodeSignals_Generated
    // [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    // private void OnStartPressed() { }

    /// <summary>
    ///     Godot 节点就绪回调：节点注入 → 信号绑定 → 异步初始化 → 事件注册
    /// </summary>
    public override void _Ready()
    {
        // __InjectGetNodes_Generated();    // 启用 [GetNode] 字段后取消注释
        // __BindNodeSignals_Generated();   // 启用 [BindNodeSignal] 后取消注释
        _ = ReadyAsync();
        RegisterEvents();
    }

    /// <summary>
    ///     节点退出场景树：解绑 [BindNodeSignal] 信号（防泄漏；启用后取消注释）
    /// </summary>
    // public override void _ExitTree()
    // {
    //     __UnbindNodeSignals_Generated();
    // }

    /// <summary>
    ///     异步等待架构就绪，获取 UI 路由器依赖
    /// </summary>
    private async Task ReadyAsync()
    {
        await GameEntryPoint.Architecture.WaitUntilReadyAsync().ConfigureAwait(false);
        // _uiRouter = this.GetSystem<IUiRouter>()!;  或 [GetSystem] 字段注入
        _log.Debug("_CLASS_ 初始化完成");
    }

    /// <summary>
    ///     注册 CQRS 事件订阅（模板用户在此处添加事件处理逻辑）
    /// </summary>
    private void RegisterEvents()
    {
        // 示例：this.RegisterEvent<SomeEvent>(e => { ... })
        //     .UnRegisterWhenNodeExitTree(this);
    }
}
