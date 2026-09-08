using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Environment;
using GFramework.Godot.Architectures;
using BreakOut.scripts.module;
using BreakOut.scripts.utility.@event;
using Microsoft.Extensions.DependencyInjection;

namespace BreakOut.scripts.core;

/// <summary>
///     游戏架构类，负责安装和管理游戏所需的各种模块
///     继承自AbstractArchitecture，用于构建游戏的整体架构体系
/// </summary>
public sealed class GameArchitecture(IArchitectureConfiguration configuration, IEnvironment environment) : AbstractArchitecture(configuration, environment)
{
    public IArchitectureConfiguration Configuration { get; } = configuration;

    /// <summary>
    ///     容器配置器：在框架内置模块注册后，用频段版事件总线覆盖原版 IEventBus。
    ///     新版框架（0.7.1+）的 EventBus 属性从容器动态获取，此处覆盖后原版
    ///     RegisterEvent / SendEvent 直接支持频段。
    /// </summary>
    public override Action<IServiceCollection>? Configurator =>
        services =>
        {
            var bus = new ChannelEventBus();
            // 同时注册具体类型与接口，确保 GetService<ChannelEventBus>() 和 GetService<IEventBus>() 都能解析
            services.AddSingleton(bus);
            services.AddSingleton(typeof(IEventBus), bus);
        };

    /// <summary>
    ///     安装游戏所需的各个功能模块
    ///     该方法在架构初始化时被调用，用于注册系统、模型和工具模块
    /// </summary>
    protected override void InstallModules()
    {
        // 安装工具类相关的Godot模块（必须最先安装，ModelModule 依赖 ISettingsDataRepository）
        InstallModule(new UtilityModule());
        // 安装系统相关的Godot模块
        InstallModule(new SystemModule());
        // 安装数据模型相关的Godot模块
        InstallModule(new ModelModule());
        // 安装状态相关的Godot模块
        InstallModule(new StateModule());
    }
}
