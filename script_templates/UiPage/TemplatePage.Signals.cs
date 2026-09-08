using GFramework.Godot.SourceGenerators.Abstractions;
using Godot;

namespace BreakOut.scripts.menu;

public partial class TemplatePage
{
    /// <summary>
    ///     [BindNodeSignal] 语法糖：把节点字段的 CLR 事件绑定/解绑收敛为生成方法。
    ///     第一个参数是节点字段（来自 .Dependencies.cs 的 [GetNode]），第二个是信号名。
    ///     回调里可桥接 CQRS 事件：this.SendEvent(new SomeEvent { ... })
    /// </summary>
    [BindNodeSignal(nameof(_startButton), nameof(Button.Pressed))]
    private void OnStartPressed()
    {
        // this.SendEvent(new StartButtonPressedEvent { ... });
    }

    [BindNodeSignal(nameof(_startButton), nameof(Button.MouseEntered))]
    private void OnStartHovered()
    {
    }
}
