using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.scoring.@event;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     分数视图：挂 score.tscn 根，订阅 ScoreChangedEvent 刷新分数 Label（原版 score.gd）。
/// </summary>
[Log]
[ContextAware]
public partial class ScoreView : Control
{
    private Label _scoreLabel = null!;

    /// <summary>
    ///     初始化：引用 Label 并订阅事件。
    /// </summary>
    public override void _Ready()
    {
        _scoreLabel = GetNodeOrNull<Label>("Score") ?? new Label();

        this.RegisterEvent<ScoreChangedEvent>(ChannelConstants.Gameplay, OnScoreChanged)
            .UnRegisterWhenNodeExitTree(this);
    }

    private void OnScoreChanged(ScoreChangedEvent e)
    {
        _scoreLabel.Text = e.Score.ToString();
    }
}
