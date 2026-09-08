using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.run.@event;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.entities.ui;

/// <summary>
///     生命条视图：挂 health.tscn 根，订阅 BallLostEvent 按剩余生命切换心形纹理（原版 health.gd）。
/// </summary>
[Log]
[ContextAware]
public partial class HealthBarView : Control
{
    private Texture2D _full = null!;
    private Texture2D _empty = null!;
    private Godot.Collections.Array<Node> _hearts = new();

    /// <summary>
    ///     初始化：加载纹理、引用心形并订阅事件。
    /// </summary>
    public override void _Ready()
    {
        _full = GD.Load<Texture2D>("res://assets/texture/ui/HeartFull.png");
        _empty = GD.Load<Texture2D>("res://assets/texture/ui/HeartEmpty.png");

        var vbox = GetNodeOrNull<VBoxContainer>("VBox");
        if (vbox != null)
        {
            _hearts = vbox.GetChildren();
        }

        this.RegisterEvent<BallLostEvent>(ChannelConstants.Gameplay, OnBallLost)
            .UnRegisterWhenNodeExitTree(this);
    }

    private void OnBallLost(BallLostEvent e)
    {
        for (var i = 0; i < _hearts.Count; i++)
        {
            if (_hearts[i] is not TextureRect heart)
            {
                continue;
            }

            heart.Texture = i < e.RemainingHealth ? _full : _empty;
        }
    }
}
