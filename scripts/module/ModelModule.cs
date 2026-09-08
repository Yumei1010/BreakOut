using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Functional.Pipe;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Setting;
using GFramework.Godot.Setting;
using GFramework.Godot.Setting.Data;
using BreakOut.scripts.data.setting;

namespace BreakOut.scripts.module;

/// <summary>
/// ModelModule 类继承自 AbstractModule，用于在架构中安装和注册模型。
/// </summary>
public class ModelModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        var settingsDataRepository = architecture.Context.GetUtility<ISettingsDataRepository>()!;

        // 注册设置模型，并配置其应用器
        architecture.RegisterModel(
            new SettingsModel<ISettingsDataRepository>(new SettingDataLocationProvider(), settingsDataRepository)
                .Also(it =>
                {
                    it.RegisterApplicator(new GodotAudioSettings(it, new AudioBusMap()));
                    it.RegisterApplicator(new GodotGraphicsSettings(it));
                    it.RegisterApplicator(new GodotLocalizationSettings(it, new LocalizationMap()));
                })
        );
    }
}
