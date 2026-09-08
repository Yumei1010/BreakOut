using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.bump.@event;
using BreakOut.scripts.cqrs.scoring.@event;
using BreakOut.scripts.domain.bump;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     动态反馈层：订阅 CQRS 事件展示连击大字与 bump 判定浮字。
/// </summary>
/// <remarks>
///     纯事件驱动表现层（不引用 GameRoot 逻辑）：
///     <list type="bullet">
///         <item>ScoreChangedEvent → 连击 &gt; 1 时居中弹「COMBO N」缩放动画，2s 无新事件自动隐藏</item>
///         <item>BumpJudgedEvent → 在板位置弹出 PERFECT/EARLY/LATE/TOO FAR 旋转上浮文字</item>
///     </list>
/// </remarks>
[Log]
[ContextAware]
public partial class FeedbackLayer : Control
{
    private Label _comboLabel = null!;
    private Timer _comboTimer = null!;
    private int _lastCombo;

    /// <summary>
    ///     初始化并订阅事件。
    /// </summary>
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        BuildComboUi();

        this.RegisterEvent<ScoreChangedEvent>(ChannelConstants.Gameplay, OnScoreChanged)
            .UnRegisterWhenNodeExitTree(this);

        this.RegisterEvent<BumpJudgedEvent>(ChannelConstants.Gameplay, OnBumpJudged)
            .UnRegisterWhenNodeExitTree(this);
    }

    private void OnScoreChanged(ScoreChangedEvent e)
    {
        if (e.Combo <= 1)
        {
            _comboLabel.Visible = false;
            _comboTimer.Stop();
            return;
        }

        _comboLabel.Visible = true;
        _comboLabel.Text = $"COMBO {e.Combo}";
        _lastCombo = e.Combo;

        // 弹入动画：重置缩放后放大到 1
        _comboLabel.Scale = new Vector2(0.4f, 0.4f);
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_comboLabel, "scale", Vector2.One, 0.25);
        tween.TweenProperty(_comboLabel, "modulate:a", 1.0f, 0.1);

        _comboTimer.Start(2.0);
    }

    private void OnBumpJudged(BumpJudgedEvent e)
    {
        if (e.Grade == BumpGrade.TooFar)
        {
            return; // 距离过远不提示
        }

        SpawnBumpText(e.Grade);
    }

    /// <summary>
    ///     生成 bump 判定浮字（随机旋转上浮后淡出）。
    /// </summary>
    private void SpawnBumpText(BumpGrade grade)
    {
        var text = grade switch
        {
            BumpGrade.Perfect => "PERFECT",
            BumpGrade.Late => "LATE",
            _ => "EARLY"
        };
        var color = grade switch
        {
            BumpGrade.Perfect => new Color(1f, 0.9f, 0.2f),
            BumpGrade.Late => new Color(1f, 0.6f, 0.3f),
            _ => new Color(0.6f, 0.8f, 1f)
        };

        var label = new Label
        {
            Text = text,
            Position = new Vector2(900, 760),
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", 42);
        label.PivotOffset = new Vector2(label.Size.X / 2, label.Size.Y / 2);
        AddChild(label);

        var tween = CreateTween().SetParallel(true);
        tween.TweenProperty(label, "rotation", (float)GD.RandRange(-0.4, 0.4), 1.2)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "position", label.Position + new Vector2((float)GD.RandRange(-100, 100), -80), 1.2)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "scale", Vector2.One, 0.5)
            .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        tween.Chain().TweenProperty(label, "modulate:a", 0f, 0.3)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }

    private void BuildComboUi()
    {
        _comboLabel = new Label
        {
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "COMBO",
            Position = new Vector2(780, 500),
            Modulate = new Color(1f, 0.85f, 0.2f)
        };
        _comboLabel.AddThemeFontSizeOverride("font_size", 64);
        _comboLabel.PivotOffset = new Vector2(_comboLabel.Size.X / 2, _comboLabel.Size.Y / 2);
        AddChild(_comboLabel);

        _comboTimer = new Timer { OneShot = true, WaitTime = 2.0 };
        _comboTimer.Timeout += () =>
        {
            var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(_comboLabel, "scale", Vector2.Zero, 0.3);
            tween.TweenProperty(_comboLabel, "modulate:a", 0f, 0.2);
        };
        AddChild(_comboTimer);
    }
}
