using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.ability;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.presentation.game;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.ball;

namespace BreakOut.scripts.presentation.paddle;

/// <summary>
///     板视图（薄壳）：Godot 物理载体，输入与能力触发转发给 domain。
/// </summary>
/// <remarks>
///     只负责表现层职责：读取输入、移动板、播放能力动画/粒子；能力规则判定走 domain AbilityRule。
///     球吸附跟随由 <see cref="LaunchPoint"/> 提供挂点。
/// </remarks>
[Log]
[ContextAware]
public partial class PaddleView : CharacterBody2D
{
    /// <summary>
    ///     板水平速度（px/s）。
    /// </summary>
    public const float MoveSpeed = 900f;

    private GameRoot _root = null!;
    private bool _dashCooldownReady = true;

    /// <summary>
    ///     获取发射点（球吸附位置）。
    /// </summary>
    public Marker2D LaunchPoint { get; private set; } = null!;

    /// <summary>
    ///     获取或设置当前吸附的球（null 表示无吸附）。
    /// </summary>
    public BallView? AttachedBall { get; set; }

    /// <summary>
    ///     初始化视图：构建板节点。
    /// </summary>
    public override void _Ready()
    {
        _root = GetParent<GameRoot>();
        BuildVisual();
        LaunchPoint = new Marker2D
        {
            Name = "LaunchPoint",
            Position = new Vector2(0, -30)
        };
        AddChild(LaunchPoint);
    }

    /// <summary>
    ///     每帧处理输入：移动与能力键。
    /// </summary>
    public override void _Process(double delta)
    {
        if (GetTree().Paused)
        {
            return;
        }

        var dir = Input.GetActionStrength("right") - Input.GetActionStrength("left");
        Velocity = new Vector2(dir * MoveSpeed, 0);

        if (Input.IsActionJustPressed("dash") && _dashCooldownReady)
        {
            if (AbilityRule.TryDash(true) == AbilityResult.Success)
            {
                _dashCooldownReady = false;
                // 表现：冲刺动画（简化，先平移加速）
                Velocity = new Vector2(Mathf.Sign(dir == 0 ? 1 : dir) * 1500f, 0);
                GetTree().CreateTimer(0.1).Timeout += () => _dashCooldownReady = true;
            }
        }

        if (Input.IsActionJustPressed("bump"))
        {
            if (AttachedBall != null)
            {
                AttachedBall.Launch();
                AttachedBall = null;
                _root.Run.StartBall();
            }
            else if (_root.Ball != null)
            {
                _root.Ball.TryBump(this);
            }
        }

        if (Input.IsActionJustPressed("special"))
        {
            if (AbilityRule.TryLaser(_root.Run) == AbilityResult.Success)
            {
                _root.Run.SpendEnergy(RunState.MaxEnergy);
                _log.Debug("激光发射");
            }
        }
    }

    /// <summary>
    ///     每物理帧移动（由物理引擎处理，这里保持空实现以便扩展）。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        // 球吸附时跟随板移动在 BallView 处理
    }

    /// <summary>
    ///     球碰板时的弹跳表现（占位：后续 tween/音效）。
    /// </summary>
    public void PlayBounce()
    {
        // 占位：后续接入 juice 表现
    }

    /// <summary>
    ///     构建板视觉（占位：渐变矩形，后续替换为 Paddle.png）。
    /// </summary>
    private void BuildVisual()
    {
        var sprite = new Sprite2D
        {
            Texture = GameTextures.Paddle,
            Scale = new Vector2(0.5f, 0.5f)
        };
        AddChild(sprite);

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(192, 24) }
        };
        AddChild(shape);

        AddToGroup("Paddle");
    }
}
