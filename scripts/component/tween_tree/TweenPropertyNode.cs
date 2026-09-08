using Godot;

namespace BreakOut.scripts.component.tween_tree;

/// <summary>
///     属性动画叶子节点：把目标节点的某个属性补间到目标值。
///     支持 position / modulate / scale / rotation 等任意属性（按 <see cref="Property"/> 名与目标值类型）。
/// </summary>
/// <remarks>
///     场景树示例：
///     <code>
///     TweenPropertyNode
///       TargetNode = %Card          （目标节点路径）
///       Property   = "position"
///       TargetValue = (300, 0)       （目标值）
///       Duration   = 0.5
///     </code>
/// </remarks>
public partial class TweenPropertyNode : TweenNode
{
    /// <summary>
    ///     目标节点路径（相对本节点或 % 唯一名）。留空则对本节点自身动画。
    /// </summary>
    [Export] public NodePath TargetNode { get; set; } = new();

    /// <summary>
    ///     要补间的属性名（如 "position"、"modulate"、"scale"、"rotation"、"rotation_degrees"）。
    /// </summary>
    [Export] public string Property { get; set; } = "position";

    /// <summary>
    ///     属性目标值（Variant：position→Vector2、modulate→Color、scale→Vector2、rotation→float 等）。
    /// </summary>
    [Export] public Variant TargetValue { get; set; }

    /// <summary>
    ///     动画时长（秒）。
    /// </summary>
    [Export(PropertyHint.Range, "0.01,10,0.01")] public float Duration { get; set; } = 0.5f;

    /// <summary>
    ///     过渡曲线类型。
    /// </summary>
    [Export] public Tween.TransitionType Trans { get; set; } = Tween.TransitionType.Quad;

    /// <summary>
    ///     缓动类型。
    /// </summary>
    [Export] public Tween.EaseType Ease { get; set; } = Tween.EaseType.InOut;

    /// <summary>
    ///     是否相对当前值（例如 position 相对位移、modulate 增量）。
    /// </summary>
    [Export] public bool AsRelative { get; set; }

    /// <summary>
    ///     解析目标节点：优先用 TargetNode 路径；留空返回本节点。
    /// </summary>
    /// <returns>目标节点</returns>
    private Node ResolveTarget()
    {
        return TargetNode.IsEmpty ? this : GetNode(TargetNode);
    }

    /// <inheritdoc />
    public override Tween BuildTween()
    {
        var tween = CreateTween();
        tween.SetTrans(Trans);
        tween.SetEase(Ease);
        var tweener = tween.TweenProperty(ResolveTarget(), new NodePath(Property), TargetValue, Duration);
        if (AsRelative)
            tweener.AsRelative();
        return tween;
    }
}
