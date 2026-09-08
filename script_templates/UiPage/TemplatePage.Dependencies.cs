using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;
using BreakOut.global;

namespace BreakOut.scripts.menu;

public partial class TemplatePage
{
    /// <summary>
    ///     UI 路由器（架构依赖注入）
    /// </summary>
    [GetSystem]
    private IUiRouter _uiRouter = null!;

    /// <summary>
    ///     页面节点引用：[GetNode] 按字段名推断 %唯一名（_titleLabel → %TitleLabel）
    ///     供 [BindNodeSignal] 信号绑定使用
    /// </summary>
    [GetNode]
    private Label _titleLabel = null!;

    [GetNode]
    private Button _startButton = null!;

    /// <summary>
    ///     异步等待架构就绪（若页面逻辑依赖就绪后的系统/模型可在此继续）
    /// </summary>
    private async Task ReadyAsync()
    {
        await GameEntryPoint.Architecture.WaitUntilReadyAsync().ConfigureAwait(false);
        _log.Debug("TemplatePage 初始化完成");
    }
}
