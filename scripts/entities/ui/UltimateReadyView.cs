using Godot;

namespace BreakOut.scripts.entities.ui;

/// <summary>
///     终极就绪提示：挂 ultimate_ready.tscn，弹性放大后自动消失（原版 ultimate_ready.gd）。
/// </summary>
public partial class UltimateReadyView : Control
{
    /// <summary>
    ///     初始化并播放提示动画。
    /// </summary>
    public override void _Ready()
    {
        Scale = Vector2.Zero;
        GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer")?.Play();
        var shaker = GetNodeOrNull("Shaker");
        shaker?.Call("start", 1.5);

        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One, 0.7)
            .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.6)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.4)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
