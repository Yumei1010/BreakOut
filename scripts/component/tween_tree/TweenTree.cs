using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using Godot;

namespace BreakOut.scripts.component.tween_tree;

/// <summary>
///     Tween 动画树根节点：挂在场景任意位置，把其子 <see cref="TweenNode"/> 组成的动画树播放出来。
///     直接子节点按顺序执行（顶层序列）；需要并行/嵌套时用组合节点（Sequence/Parallel）组织。
/// </summary>
/// <remarks>
///     场景树示例：
///     <code>
///     TweenTree
///     ├── TweenPropertyNode (卡牌移到台面)
///     └── TweenSequenceNode
///         ├── TweenPropertyNode (翻牌：scale.x 0→1)
///         └── TweenPropertyNode (发光：modulate)
///
///     // 播放：
///     GetNode&lt;TweenTree&gt;("%CardAnim").Play();
///     // 或等完成：
///     await GetNode&lt;TweenTree&gt;("%CardAnim").PlayAsync();
///     </code>
/// </remarks>
[Log]
public partial class TweenTree : TweenNode
{
    private Tween? _current;

    /// <summary>
    ///     是否正在播放。
    /// </summary>
    public bool IsPlaying => _current?.IsValid() == true && _current.IsRunning();

    /// <summary>
    ///     播放动画树（若正在播放会先停止并重建）。
    /// </summary>
    /// <returns>本次播放的 Tween</returns>
    public override Tween BuildTween()
    {
        var tween = CreateTween();
        var first = true;
        foreach (var child in ChildNodes)
        {
            if (!first)
                tween.Chain(); // 保证顶层子节点按顺序（并行模式时保险）
            tween.TweenSubtween(child.BuildTween());
            first = false;
        }

        return tween;
    }

    /// <summary>
    ///     开始播放（播放前自动 Kill 上一次未完成的动画）。
    /// </summary>
    public new void Play()
    {
        Kill();
        _current = BuildTween();
        _log.Debug($"TweenTree 播放 {Name}");
    }

    /// <summary>
    ///     停止并清理当前动画（目标属性停在当前值）。
    /// </summary>
    public void Stop()
    {
        _current?.Stop();
    }

    /// <summary>
    ///     立即终止当前动画。
    /// </summary>
    public void Kill()
    {
        if (_current?.IsValid() == true)
        {
            _current.Kill();
        }

        _current = null;
    }

    /// <summary>
    ///     播放并等待动画树全部完成（配合 ActionQueue 或 async/await）。
    /// </summary>
    /// <returns>播放完成的等待任务</returns>
    public async Task PlayAsync()
    {
        Play();
        if (_current != null)
        {
            await ToSignal(_current, Tween.SignalName.Finished);
        }
    }
}
