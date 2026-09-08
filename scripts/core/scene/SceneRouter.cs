using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Scene;
using GFramework.Game.Scene.Handler;
using Godot;

namespace BreakOut.scripts.core.scene;

/// <summary>
/// 场景路由器类，负责管理游戏场景的切换和路由功能
/// 继承自SceneRouterBase基类，提供场景根节点访问和初始化功能
/// </summary>
public class SceneRouter : SceneRouterBase
{
    /// <summary>
    /// 获取场景根节点
    /// 将基类的Root属性转换为Node类型返回
    /// </summary>
    public Node? SceneRoot => Root as Node;

    /// <summary>
    /// 注册默认的场景过渡处理器
    /// </summary>
    protected override void RegisterHandlers()
    {
        RegisterHandler(new LoggingTransitionHandler(), new SceneTransitionHandlerOptions());
    }

    /// <summary>
    /// 初始化方法，在场景路由器创建时调用
    /// 可在此处添加场景路由相关的初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
    }
}
