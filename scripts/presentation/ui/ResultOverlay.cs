using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.brick.@event;
using BreakOut.scripts.cqrs.run.@event;
using BreakOut.scripts.presentation.game;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     结算面板：订阅通关/游戏结束事件展示结果并提供重开/下一关。
/// </summary>
/// <remarks>
///     纯事件驱动，不引用 GameRoot 逻辑；重开通过 Godot 场景重载实现（简单可靠）。
/// </remarks>
[Log]
[ContextAware]
public partial class ResultOverlay : Control
{
	private Label _titleLabel = null!;
	private Label _detailLabel = null!;
	private Label _hintLabel = null!;
	private bool _gameOver;

	/// <summary>
	///     初始化并订阅事件。
	/// </summary>
	public override void _Ready()
	{
		Visible = false;
		SetProcessInput(false);
		BuildLayout();

		this.RegisterEvent<BallLostEvent>(ChannelConstants.Gameplay, OnBallLost)
			.UnRegisterWhenNodeExitTree(this);

		this.RegisterEvent<BrickDestroyedEvent>(ChannelConstants.Gameplay, OnBrickDestroyed)
			.UnRegisterWhenNodeExitTree(this);
	}

	private void OnBallLost(BallLostEvent e)
	{
		if (!e.GameOver)
		{
			return;
		}

		_gameOver = true;
		_titleLabel.Text = "游戏结束";
		_detailLabel.Text = "再接再厉！";
		ShowOverlay();
	}

	private void OnBrickDestroyed(BrickDestroyedEvent e)
	{
		if (!e.LevelCleared)
		{
			return;
		}

		_gameOver = false;
		_titleLabel.Text = "关卡清除！";
		_detailLabel.Text = "准备进入下一关";
		ShowOverlay();
	}

	private void ShowOverlay()
	{
		Visible = true;
		SetProcessInput(true);
		_hintLabel.Text = "按空格重开";
		_log.Info($"结算面板显示：{_titleLabel.Text}");
	}

	/// <inheritdoc />
	public override void _UnhandledInput(InputEvent? @event)
	{
		if (!Visible || @event == null)
		{
			return;
		}

		if (@event.IsActionPressed("bump") || @event.IsActionPressed("ui_accept"))
		{
			ReloadGame();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void ReloadGame()
	{
		GetTree().ReloadCurrentScene();
	}

	/// <summary>
	///     构建结算面板布局。
	/// </summary>
	private void BuildLayout()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		var dim = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 0.6f)
		};
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		var center = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		center.SetAnchorsPreset(LayoutPreset.Center);
		center.GrowHorizontal = Control.GrowDirection.Both;
		center.GrowVertical = Control.GrowDirection.Both;
		AddChild(center);

		_titleLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
		_titleLabel.AddThemeFontSizeOverride("font_size", 64);
		center.AddChild(_titleLabel);

		_detailLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
		_detailLabel.AddThemeFontSizeOverride("font_size", 28);
		center.AddChild(_detailLabel);

		_hintLabel = new Label { Text = "", HorizontalAlignment = HorizontalAlignment.Center };
		_hintLabel.AddThemeFontSizeOverride("font_size", 22);
		center.AddChild(_hintLabel);
	}
}
