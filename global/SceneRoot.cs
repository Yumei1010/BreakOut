using GFramework.Core.Extensions;
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using Godot;

namespace BreakOut.global;

/// <summary>
///     场景根节点：承载场景行为（ISceneBehavior）的容器。
///     由 SceneRouter 通过 BindRoot 绑定，场景切换时 AddScene/RemoveScene 被调用。
/// </summary>
[Log]
[ContextAware]
public partial class SceneRoot : Node2D, ISceneRoot
{
    private ISceneBehavior? _current;

    /// <summary>
    ///     添加场景行为：把其原始节点挂载到本节点下。
    /// </summary>
    /// <param name="scene">场景行为实例</param>
    public void AddScene(ISceneBehavior scene)
    {
        if (scene.Original is not Node node)
            throw new InvalidOperationException("SceneBehavior.Original must be a Godot Node");

        RemoveSceneInternal();
        AddChild(node);
        _current = scene;
    }

    /// <summary>
    ///     移除场景行为：卸载其原始节点。
    /// </summary>
    /// <param name="scene">场景行为实例</param>
    public void RemoveScene(ISceneBehavior scene)
    {
        if (scene.Original is not Node node)
            return;

        node.QueueFreeX();
        _current = null;
    }

    /// <summary>
    ///     移除当前场景。
    /// </summary>
    private void RemoveSceneInternal()
    {
        if (_current is null)
            return;

        if (_current.Original is Node node)
            node.QueueFreeX();
        _current = null;
    }

    /// <summary>
    ///     节点就绪：获取场景注册表并绑定场景路由。
    /// </summary>
    public override void _Ready()
    {
        var router = this.GetSystem<ISceneRouter>()!;
        router.BindRoot(this);
        this.SendEvent<SceneRootReadyEvent>();
    }

    /// <summary>
    ///     场景根就绪事件。
    /// </summary>
    public sealed class SceneRootReadyEvent;
}
