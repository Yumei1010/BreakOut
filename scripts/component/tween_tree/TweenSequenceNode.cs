using Godot;

namespace BreakOut.scripts.component.tween_tree;

/// <summary>
///     顺序节点：子节点逐个执行（前一个动画完成才播下一个）。
///     内部用 <see cref="Tween.TweenSubtween"/> 把每个子节点的 Tween 作为一步嵌入。
/// </summary>
/// <remarks>
///     场景树示例：
///     <code>
///     TweenSequenceNode
///     ├── TweenPropertyNode (移到位置 A)
///     ├── TweenPropertyNode (淡出)
///     └── TweenIntervalNode  (停 0.3s)
///     </code>
/// </remarks>
public partial class TweenSequenceNode : TweenNode
{
    /// <inheritdoc />
    public override Tween BuildTween()
    {
        var tween = CreateTween();

        foreach (var child in ChildNodes)
        {
            // 子节点各自创建独立 Tween，作为一步嵌入（引擎自动接管子 Tween 执行）
            tween.TweenSubtween(child.BuildTween());
        }

        return tween;
    }
}
