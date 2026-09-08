using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.run.@event;
using BreakOut.scripts.cqrs.scoring.@event;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     HUD 视图：订阅 CQRS 事件刷新生命/能量/分数显示。
/// </summary>
/// <remarks>
///     事件驱动示范：本节点不引用 GameRoot，仅通过 RegisterEvent 订阅规则级变化。
///     能量条/生命/分数均为代码构建的占位 Control，资产接入后仅换样式。
/// </remarks>
[Log]
[ContextAware]
public partial class HudView : Control
{
    private Label _scoreLabel = null!;
    private Label _healthLabel = null!;
    private ProgressBar _energyBar = null!;

    /// <summary>
    ///     初始化并订阅事件。
    /// </summary>
    public override void _Ready()
    {
        BuildLayout();

        this.RegisterEvent<ScoreChangedEvent>(ChannelConstants.Gameplay, e =>
        {
            _scoreLabel.Text = $"得分 {e.Score}";
            _log.Debug($"得分刷新：{e.Score}（连击 {e.Combo}）");
        }).UnRegisterWhenNodeExitTree(this);

        this.RegisterEvent<BallLostEvent>(ChannelConstants.Gameplay, e =>
        {
            _healthLabel.Text = e.GameOver
                ? "游戏结束"
                : $"生命 {e.RemainingHealth}/{RunState.MaxHealth}";
        }).UnRegisterWhenNodeExitTree(this);

        this.RegisterEvent<EnergyChangedEvent>(ChannelConstants.Gameplay, e =>
        {
            _energyBar.Value = e.Energy;
        }).UnRegisterWhenNodeExitTree(this);
    }

    /// <summary>
    ///     构建 HUD 布局（代码构建，占位样式）。
    /// </summary>
    private void BuildLayout()
    {
        _scoreLabel = new Label
        {
            Text = "得分 0",
            Position = new Vector2(40, 20)
        };
        _scoreLabel.AddThemeFontSizeOverride("font_size", 28);
        AddChild(_scoreLabel);

        _healthLabel = new Label
        {
            Text = $"生命 {RunState.MaxHealth}/{RunState.MaxHealth}",
            Position = new Vector2(40, 60)
        };
        _healthLabel.AddThemeFontSizeOverride("font_size", 28);
        AddChild(_healthLabel);

        _energyBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = RunState.MaxEnergy,
            Value = 0,
            Position = new Vector2(40, 105),
            Size = new Vector2(300, 24),
            ShowPercentage = false
        };
        _energyBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.9f, 0.6f)
        });
        AddChild(_energyBar);
    }
}
