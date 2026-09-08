using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Abstractions.State;
using GFramework.Core.Architectures;
using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting.Events;
using GFramework.Godot.Coroutine;
using GFramework.Godot.Logging;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFramework.Ecs.Arch.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFrameworkTemplate.scripts.core;
using GFrameworkTemplate.scripts.core.environment;
using GFrameworkTemplate.scripts.core.resource;
using GFrameworkTemplate.scripts.core.state.impls;
using GFrameworkTemplate.scripts.framework.logging;
using GFrameworkTemplate.scripts.enums.scene;
using GFrameworkTemplate.scripts.utility.registry;
using GFrameworkTemplate.scripts.cqrs.setting.command;
using Godot;
using Godot.Collections;

namespace GFrameworkTemplate.global;

/// <summary>
///     游戏入口点节点类，负责初始化游戏架构和管理全局游戏状态
/// </summary>
[Log]
[ContextAware]
public partial class GameEntryPoint : Node
{
    [Export] public bool IsDev { get; set; } = true;

    [Export] public Array<UiPageConfig> UiPageConfigs { get; set; } = null!;

    [Export] public Array<SceneConfig> GameSceneConfigs { get; set; } = null!;

    [Export] public Array<TextureConfig> TextureConfigs { get; set; } = null!;

    private IGodotSceneRegistry _sceneRegistry = null!;
    private ISettingsModel _settingsModel = null!;
    private ISettingsSystem _settingsSystem = null!;
    private IGodotTextureRegistry _textureRegistry = null!;
    private IGodotUiRegistry _uiRegistry = null!;
    public static IArchitecture Architecture { get; private set; } = null!;
    public static SceneTree Tree { get; private set; } = null!;

    /// <summary>
    ///     获取本次会话的日志文件信息，供日志导出与排查入口使用。
    /// </summary>
    public static SessionLogFileInfo? SessionLogFile { get; private set; }

    private static SessionFileLoggerFactoryProvider _logProvider = null!;

    public override void _Ready()
    {
        Tree = GetTree();

        // 初始化会话日志文件（先于架构创建，供 LoggerProperties 引用）
        var logDirectory = ProjectSettings.GlobalizePath("user://logs/");
        SessionLogFile = SessionLogFileHelper.CreateNow(logDirectory);
        _logProvider = new SessionFileLoggerFactoryProvider(SessionLogFile.FullPath)
        {
            MinLevel = LogLevel.Debug
        };
        LogOpenHelper.PrintSessionLogPath(SessionLogFile.FullPath);

        var arch = new GameArchitecture(new ArchitectureConfiguration
        {
            LoggerProperties = new LoggerProperties
            {
                LoggerFactoryProvider = _logProvider
            }
        }, IsDev ? new GameDevEnvironment() : new GameMainEnvironment())
        .UseArch(); // 接入 Arch ECS（World 注册进容器，可选：UseArch(options => options.WorldCapacity = 2048)）
        Architecture = arch;
        arch.Initialize();
        try { GameContext.Bind(typeof(GameArchitecture), arch.Context); }
        catch (InvalidOperationException) { /* 上下文已绑定 */ }
        _settingsModel = this.GetModel<ISettingsModel>()!;

        // 先注册事件再初始化设置，避免异步发送错过订阅（0.7.1 时序变化）
        this.RegisterEvent<SettingsInitializedEvent>(e =>
        {
            _settingsSystem = this.GetSystem<ISettingsSystem>()!;
            _ = _settingsSystem.ApplyAll();
            _log.Info("设置已加载");
        });

        _ = _settingsModel.InitializeAsync();

        _sceneRegistry = this.GetUtility<IGodotSceneRegistry>()!;
        _uiRegistry = this.GetUtility<IGodotUiRegistry>()!;
        _textureRegistry = this.GetUtility<IGodotTextureRegistry>()!;

        foreach (var gameSceneConfig in GameSceneConfigs) _sceneRegistry.Registry(gameSceneConfig);
        foreach (var uiPageConfig in UiPageConfigs) _uiRegistry.Registry(uiPageConfig);
        foreach (var textureConfig in TextureConfigs) _textureRegistry.Registry(textureConfig);

        if (ShouldEnterAppState())
            this.RegisterEvent<UiRoot.UiRootReadyEvent>(_evt =>
            {
                _ = this.GetSystem<IStateMachineSystem>()!.ChangeToAsync<AppState>();
            });

        _log.Debug("框架入口点就绪.");
        CallDeferred(nameof(CallDeferredInit));
    }

    private static void CallDeferredInit()
    {
        Timing.Prewarm();
    }

    private bool ShouldEnterAppState()
    {
        var tree = GetTree();
        var currentScene = tree.CurrentScene;
        if (currentScene == null)
            return false;
        var scenePath = currentScene.SceneFilePath;
        return string.Equals(scenePath, _sceneRegistry.Get(nameof(SceneKey.Main)).GetPath(),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     打开本次会话日志文件所在目录（供开发调试入口调用）。
    /// </summary>
    public static void OpenSessionLogDirectory()
    {
        if (SessionLogFile == null)
        {
            GD.PrintErr("[Log] 会话日志尚未初始化。");
            return;
        }

        LogOpenHelper.OpenLogDirectory(SessionLogFile.DirectoryPath);
    }

    /// <summary>
    ///     开发模式下按 F12 打开本次会话日志目录。
    /// </summary>
    /// <param name="event">未处理的输入事件。</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsDev)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true, Keycode: Key.F12 })
        {
            OpenSessionLogDirectory();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        _logProvider?.Flush();
        _ = this.SendCommandAsync(new SaveSettingsCommand());
    }
}
