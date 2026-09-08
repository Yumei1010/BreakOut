using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using BreakOut.scripts.cqrs.graphics.input;

namespace BreakOut.scripts.cqrs.graphics.command;

/// <summary>
///     切换全屏模式命令类
/// </summary>
/// <param name="input">切换全屏命令输入参数</param>
public sealed class ToggleFullscreenCommand(ToggleFullscreenCommandInput input)
    : AbstractAsyncCommand<ToggleFullscreenCommandInput>(input)
{
    /// <summary>
    ///     执行切换全屏命令
    /// </summary>
    /// <param name="input">切换全屏命令输入参数</param>
    protected override async Task OnExecuteAsync(ToggleFullscreenCommandInput input)
    {
        var model = this.GetModel<ISettingsModel>()!;
        model.GetData<GraphicsSettings>().Fullscreen = input.Fullscreen;
        await this.GetSystem<ISettingsSystem>()!.Apply<GodotGraphicsSettings>().ConfigureAwait(false);
    }
}