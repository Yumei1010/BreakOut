using Godot;

namespace BreakOut.scripts.component.tween_tree;

/// <summary>
///     并行节点：所有子节点同时开始执行。
///     第一个子节点正常嵌入，后续子节点用 <c>Parallel()</c> 标记并行后嵌入。
/// </summary>
/// <remarks>
///     场景树示例（淡入 + 放大同时进行）：
///     <code>
///     TweenParallelNode
///     ├── TweenPropertyNode (modulate → 不透明)
///     └── TweenPropertyNode (scale → 放大)
///     </code>
/// </remarks>
public partial class TweenParallelNode : TweenNode
{
    /// <inheritdoc />
    public override Tween BuildTween()
    {
        var tween = CreateTween();

        var first = true;
        foreach (var child in ChildNodes)
        {
            if (!first)
                tween.Parallel(); // 后续子节点与上一步并行
            tween.TweenSubtween(child.BuildTween());
            first = false;
        }

        return tween;
    }
}
