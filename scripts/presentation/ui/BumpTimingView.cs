using Godot;
using BreakOut.scripts.rules.bump;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     Bump 判定飘字：挂 bump_timing.tscn，按类型设文本/颜色后弹性上浮淡出自毁。
/// </summary>
/// <remarks>
///     移植自原版 bump_timing.gd：PERFECT 金黄+粒子 / EARLY/LATE 依类型配色，随机旋转上浮。
/// </remarks>
public partial class BumpTimingView : Node2D
{
    private Label _label = null!;

    /// <summary>
    ///     初始化并播放动画。
    /// </summary>
    public override void _Ready()
    {
        _label = GetNodeOrNull<Label>("Label") ?? new Label();
        PlayAnimation();
    }

    /// <summary>
    ///     设置判定类型并显示（实例化后、入树前调用）。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    public void Setup(BumpGrade grade)
    {
        if (_label == null)
        {
            return;
        }

        var text = grade switch
        {
            BumpGrade.Perfect => "PERFECT",
            BumpGrade.Late => "LATE",
            BumpGrade.Early => "EARLY",
            _ => "TOO FAR"
        };
        var color = grade switch
        {
            BumpGrade.Perfect => new Color(1f, 0.9f, 0.2f),
            BumpGrade.Late => new Color(1f, 0.6f, 0.3f),
            BumpGrade.Early => new Color(0.6f, 0.8f, 1f),
            _ => Colors.White
        };

        _label.Text = text;
        _label.Modulate = color;
        _label.Scale = Vector2.Zero;

        if (grade == BumpGrade.Perfect)
        {
            var particles = GetNodeOrNull<GpuParticles2D>("Label/GPUParticles2D");
            particles?.Restart();
        }
    }

    private void PlayAnimation()
    {
        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(_label, "rotation", (float)GD.RandRange(-0.45, 0.45), 1.2)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_label, "position", _label.Position + new Vector2((float)GD.RandRange(-100, 100), -70), 1.2)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_label, "scale", Vector2.One, 0.6)
            .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        tween.Chain().TweenProperty(_label, "modulate:a", 0f, 0.4)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
