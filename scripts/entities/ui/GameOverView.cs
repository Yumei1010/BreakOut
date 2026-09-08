using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;

namespace BreakOut.scripts.entities.ui;

/// <summary>
///     游戏结束面板：挂 game_over.tscn，Retry 重开 / Quit 退出（原版 game_over.gd）。
/// </summary>
[Log]
[ContextAware]
public partial class GameOverView : Control
{
    /// <summary>
    ///     初始化：聚焦 Retry 按钮并绑定。
    /// </summary>
    public override void _Ready()
    {
        var retry = GetNodeOrNull<Button>("VBoxContainer/RetryBtn");
        retry?.GrabFocus();
        if (retry != null)
        {
            retry.Pressed += ReloadGame;
        }

        var quit = GetNodeOrNull<Button>("VBoxContainer/QuitBtn");
        if (quit != null)
        {
            quit.Pressed += () => GetTree().Quit();
        }

        var anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (anim != null)
        {
            anim.AnimationFinished += name =>
            {
                if (name == "appear")
                {
                    anim.Play("wiggle");
                }
            };
            anim.Play("appear");
        }
    }

    private void ReloadGame()
    {
        Godot.Engine.TimeScale = 1.0f;
        GetTree().ReloadCurrentScene();
    }
}
