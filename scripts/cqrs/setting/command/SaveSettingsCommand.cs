using GFramework.Core.Command;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;

namespace BreakOut.scripts.cqrs.setting.command;

/// <summary>
///     保存游戏设置命令类
///     负责将当前游戏设置数据保存到存储中
/// </summary>
public sealed class SaveSettingsCommand : AbstractAsyncCommand
{
    /// <summary>
    ///     执行保存设置命令的主逻辑
    /// </summary>
    protected override async Task OnExecuteAsync()
    {
        await this.GetSystem<ISettingsSystem>()!.SaveAll().ConfigureAwait(false);
    }
}