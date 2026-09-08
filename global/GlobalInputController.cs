using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.core.controller;
using BreakOut.scripts.enums.input;
using Godot;

namespace BreakOut.global;

/// <summary>
///     全局输入控制器，负责处理全局输入事件
/// </summary>
[Log]
public partial class GlobalInputController : GameInputController
{
    protected override bool AcceptPhase(InputPhase phase)
    {
        return phase is InputPhase.Global or InputPhase.Paused;
    }

    protected override void Handle(InputPhase phase, InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel"))
            return;

        GetViewport().SetInputAsHandled();
    }
}
