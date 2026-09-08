using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     过关结算面板：挂 stage_clear.tscn，展示统计并进入下一关（原版 stage_clear.gd 简化）。
/// </summary>
[Log]
[ContextAware]
public partial class StageClearView : Control
{
    private GameRoot _root = null!;

    /// <summary>
    ///     初始化：引用 GameRoot 并填充统计。
    /// </summary>
    public override void _Ready()
    {
        _root = FindGameRoot();

        var next = GetNodeOrNull<Button>("VBoxContainer/NextBtn") ?? FindNextButton();
        if (next != null)
        {
            next.Pressed += ReloadGame;
            next.GrabFocus();
        }

        FillStats();
    }

    private Button? FindNextButton()
    {
        foreach (var node in FindChildren("*", "Button"))
        {
            if (node is Button b && b.Name.ToString().Contains("Next", System.StringComparison.Ordinal))
            {
                return b;
            }
        }

        return null;
    }

    private void FillStats()
    {
        if (_root == null)
        {
            return;
        }

        var result = _root.BuildStageResult();

        var early = GetNodeOrNull<Label>("VBoxContainer2/HBoxContainer/VBoxContainer3/EarlyBumps");
        if (early != null)
        {
            early.Text = result.EarlyBumps.ToString();
        }

        var late = GetNodeOrNull<Label>("VBoxContainer2/HBoxContainer/VBoxContainer3/LateBumps");
        if (late != null)
        {
            late.Text = result.LateBumps.ToString();
        }

        var perfect = GetNodeOrNull<Label>("VBoxContainer2/HBoxContainer/VBoxContainer3/PerfectBumps");
        if (perfect != null)
        {
            perfect.Text = result.PerfectBumps.ToString();
        }

        var bounces = GetNodeOrNull<Label>("VBoxContainer2/HBoxContainer/VBoxContainer3/Bounces");
        if (bounces != null)
        {
            bounces.Text = result.BallBounces.ToString();
        }

        var score = GetNodeOrNull<Label>("VBoxContainer2/HBoxContainer/VBoxContainer3/Score");
        if (score != null)
        {
            score.Text = result.RunScore.ToString();
        }
    }

    private void ReloadGame()
    {
        Godot.Engine.TimeScale = 1.0f;
        GetTree().ReloadCurrentScene();
    }

    private GameRoot FindGameRoot()
    {
        var node = GetParent();
        while (node != null)
        {
            if (node is GameRoot root)
            {
                return root;
            }

            node = node.GetParent();
        }

        return null;
    }
}
