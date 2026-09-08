using Godot;
using BreakOut.scripts.entities.game;
using BreakOut.scripts.entities.paddle;

namespace BreakOut.scripts.entities.ball;

/// <summary>
///     球实体依赖注入：节点引用解析、GameRoot 查找与场景组装。
/// </summary>
public partial class BallView
{
    private GameRoot _root = null!;
    private PaddleView _paddle = null!;

    /// <summary>
    ///     场景组装完成后初始化（由 GameRoot 显式调用，规避场景实例化顺序问题）。
    /// </summary>
    /// <param name="paddle">板视图。</param>
    public void OnSceneReady(PaddleView paddle)
    {
        _paddle = paddle;
        AttachToPaddle();
    }

    /// <summary>
    ///     解析布局场景提供的视觉子节点。
    /// </summary>
    private void ResolveVisualNodes()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        _anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        _speedParticles = GetNodeOrNull<GpuParticles2D>("SpeedParticles");
        _appearParticles = GetNodeOrNull<GpuParticles2D>("AppearParticles");
        _velocityLine = GetNodeOrNull<Line2D>("VelocityLine");
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
