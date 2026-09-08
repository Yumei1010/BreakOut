using GFramework.Core.Extensions;
using GFramework.Core.Query;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Abstractions.Setting.Data;
using BreakOut.scripts.cqrs.setting.query.result;

namespace BreakOut.scripts.cqrs.setting.query;

/// <summary>
///     获取当前设置的查询类
/// </summary>
public sealed class GetCurrentSettingsQuery : AbstractQuery<SettingsView>
{
    /// <summary>
    ///     执行获取当前设置的查询操作
    /// </summary>
    /// <returns>包含当前设置信息的SettingsView对象</returns>
    protected override SettingsView OnDo()
    {
        // 从模型中获取设置数据
        var model = this.GetModel<ISettingsModel>()!;
        // 再此可以校验设置数据
        // 构建并返回设置视图对象
        return new SettingsView
        {
            Audio = model.GetData<AudioSettings>(),
            Graphics = model.GetData<GraphicsSettings>(),
            Localization = model.GetData<LocalizationSettings>()
        };
    }
}