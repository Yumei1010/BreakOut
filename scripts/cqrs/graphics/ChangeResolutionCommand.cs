using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using BreakOut.scripts.cqrs.graphics.input;

namespace BreakOut.scripts.cqrs.graphics.command;

/// <summary>
///     更改分辨率命令类，用于处理分辨率更改操作
/// </summary>
/// <param name="input">分辨率更改命令输入参数</param>
public sealed class ChangeResolutionCommand(ChangeResolutionCommandInput input)
    : AbstractAsyncCommand<ChangeResolutionCommandInput>(input)
{
    /// <summary>
    ///     执行分辨率更改命令
    /// </summary>
    /// <param name="input">分辨率更改命令输入参数，包含新的宽度和高度值</param>
    protected override async Task OnExecuteAsync(ChangeResolutionCommandInput input)
    {
        var model = this.GetModel<ISettingsModel>()!;
        var settings = model.GetData<GraphicsSettings>();
        settings.ResolutionWidth = input.Width;
        settings.ResolutionHeight = input.Height;
        await this.GetSystem<ISettingsSystem>()!.Apply<GodotGraphicsSettings>().ConfigureAwait(false);
    }
}