using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using BreakOut.scripts.cqrs.audio.command.input;

namespace BreakOut.scripts.cqrs.audio.command;

/// <summary>
///     更改背景音乐音量命令类，用于处理BGM音量更改操作
/// </summary>
/// <param name="input">背景音乐音量更改命令输入参数</param>
public sealed class ChangeBgmVolumeCommand(ChangeBgmVolumeCommandInput input)
    : AbstractAsyncCommand<ChangeBgmVolumeCommandInput>(input)
{
    /// <summary>
    ///     执行背景音乐音量更改命令
    /// </summary>
    /// <param name="input">背景音乐音量更改命令输入参数，包含新的音量值</param>
    protected override async Task OnExecuteAsync(ChangeBgmVolumeCommandInput input)
    {
        var model = this.GetModel<ISettingsModel>()!;
        model.GetData<AudioSettings>().BgmVolume = input.Volume;
        await this.GetSystem<ISettingsSystem>()!.Apply<GodotAudioSettings>().ConfigureAwait(false);
    }
}