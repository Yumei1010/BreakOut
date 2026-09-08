using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using GFramework.Godot.Setting;
using BreakOut.scripts.cqrs.setting.command.input;

namespace BreakOut.scripts.cqrs.setting.command;

/// <summary>
///     更改语言命令类
///     用于处理更改语言的业务逻辑
/// </summary>
/// <param name="input">语言更改命令输入参数</param>
public sealed class ChangeLanguageCommand(ChangeLanguageCommandInput input)
    : AbstractAsyncCommand<ChangeLanguageCommandInput>(input)
{
    /// <summary>
    ///     执行命令的异步方法
    /// </summary>
    /// <param name="input">命令输入参数</param>
    protected override async Task OnExecuteAsync(ChangeLanguageCommandInput input)
    {
        var model = this.GetModel<ISettingsModel>()!;
        var settings = model.GetData<LocalizationSettings>();
        settings.Language = input.Language;
        await this.GetSystem<ISettingsSystem>()!.Apply<GodotLocalizationSettings>().ConfigureAwait(false);
    }
}