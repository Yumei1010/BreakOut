using GFramework.Core.Abstractions.Architectures;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Data;
using GFramework.Game.Serializer;
using GFramework.Godot.Scene;
using GFramework.Godot.Storage;
using GFramework.Godot.UI;
using BreakOut.scripts.utility.registry;
using Godot;

namespace BreakOut.scripts.module;

/// <summary>
///     工具模块类，负责安装和管理框架中的实用工具组件
/// </summary>
public class UtilityModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        architecture.RegisterUtility(new GodotUiRegistry());
        architecture.RegisterUtility(new GodotSceneRegistry());
        architecture.RegisterUtility(new GodotTextureRegistry());
        architecture.RegisterUtility(new GodotUiFactory());

        var jsonSerializer = new JsonSerializer();
        architecture.RegisterUtility(jsonSerializer);

        var storage = new GodotFileStorage(jsonSerializer);
        architecture.RegisterUtility(storage);

        architecture.RegisterUtility(new UnifiedSettingsDataRepository(storage, jsonSerializer,
            new DataRepositoryOptions
            {
                BasePath = ProjectSettings.GetSetting("application/config/save/setting_path").AsString(),
                AutoBackup = true
            }));
    }
}
