using GFramework.Core.Abstractions.Architectures;
using GFramework.Game.Setting;
using BreakOut.scripts.core.scene;
using BreakOut.scripts.core.ui;

namespace BreakOut.scripts.module;

/// <summary>
///     系统模块类，负责安装和注册框架所需的各种系统组件
/// </summary>
public class SystemModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new UiRouter());
        architecture.RegisterSystem(new SceneRouter());
        architecture.RegisterSystem(new SettingsSystem());
    }
}
