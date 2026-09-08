using Godot;
using BreakOut.scripts.entities.game;

namespace BreakOut.scripts.entities.paddle;

/// <summary>
///     板实体依赖注入：节点引用解析与 GameRoot 查找。
/// </summary>
public partial class PaddleView
{
    private GameRoot _root = null!;
    private AnimationPlayer _anim = null!;
    private Sprite2D _sprite = null!;
    private LaserView? _laser;

    /// <summary>
    ///     解析布局场景提供的子节点。
    /// </summary>
    private void ResolveVisualNodes()
    {
        LaunchPoint = GetNodeOrNull<Marker2D>("LaunchPoint") ?? new Marker2D { Position = new Vector2(0, -37) };
        if (GetNodeOrNull<Marker2D>("LaunchPoint") == null)
        {
            AddChild(LaunchPoint);
        }

        _sprite = GetNodeOrNull<Sprite2D>("Paddle");
        _anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _laser = GetNodeOrNull<LaserView>("Laser");
        _visualReady = _sprite != null;
    }

    /// <summary>
    ///     向上查找 GameRoot。
    /// </summary>
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
