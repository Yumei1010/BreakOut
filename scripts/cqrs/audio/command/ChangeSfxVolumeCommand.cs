using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using BreakOut.scripts.cqrs.audio.command.input;

namespace BreakOut.scripts.cqrs.audio.command;

/// <summary>
///     更改音效音量命令类，用于处理SFX音量更改操作
/// </summary>
/// <param name="input">音效音量更改命令输入参数</param>
public sealed class ChangeSfxVolumeCommand(ChangeSfxVolumeCommandInput input)
    : AbstractAsyncCommand<ChangeSfxVolumeCommandInput>(input)
{
    /// <summary>
    ///     执行音效音量更改命令
    /// </summary>
    /// <param name="input">音效音量更改命令输入参数，包含新的音量值</param>
    protected override async Task OnExecuteAsync(ChangeSfxVolumeCommandInput input)
    {
        var model = this.GetModel<ISettingsModel>()!;
        model.GetData<AudioSettings>().SfxVolume = input.Volume;
        await this.GetSystem<ISettingsSystem>()!.Apply<GodotAudioSettings>().ConfigureAwait(false);
    }
}