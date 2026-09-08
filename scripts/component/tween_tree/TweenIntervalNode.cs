using Godot;

namespace BreakOut.scripts.component.tween_tree;

/// <summary>
///     延时叶子节点：在序列中插入一段停顿（如"等 0.5 秒再播下一段"）。
/// </summary>
public partial class TweenIntervalNode : TweenNode
{
    /// <summary>
    ///     停顿时长（秒）。
    /// </summary>
    [Export(PropertyHint.Range, "0.01,10,0.01")] public float Duration { get; set; } = 0.5f;

    /// <inheritdoc />
    public override Tween BuildTween()
    {
        var tween = CreateTween();
        tween.TweenInterval(Duration);
        return tween;
    }
}
