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
    private bool _magnetCooldownReady = true;
    private Sprite2D _sprite = null!;

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
                // 表现：冲刺加速 + 残影
                Velocity = new Vector2(Mathf.Sign(dir == 0 ? 1 : dir) * 1500f, 0);
                SpawnGhosts();
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

        // Magnet（新能力）：按 attract 键召回球回板（免能量）
        if (Input.IsActionJustPressed("attract"))
        {
            if (AbilityRule.TryMagnet(_magnetCooldownReady) != AbilityResult.Success)
            {
                _log.Debug("Magnet 冷却中");
            }
            else if (_root.Ball is { Dead: false } ball)
            {
                _magnetCooldownReady = false;
                GetTree().CreateTimer(2.0).Timeout += () => _magnetCooldownReady = true;
                ball.AttachToPaddle();
                _log.Debug("Magnet 召回球");
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
    ///     冲刺残影：克隆板贴图逐帧落后并淡出（原版 ghost_spawner 简化）。
    /// </summary>
    private void SpawnGhosts()
    {
        for (var i = 0; i < 3; i++)
        {
            var delay = i * 0.04;
            var ghost = new Sprite2D
            {
                Texture = _sprite.Texture,
                Scale = _sprite.Scale,
                GlobalPosition = GlobalPosition,
                Modulate = new Color(0.7f, 0.8f, 1f, 0.6f)
            };
            GetParent().AddChild(ghost);

            var tween = CreateTween();
            tween.TweenInterval(delay);
            tween.TweenProperty(ghost, "modulate:a", 0f, 0.25);
            tween.TweenCallback(Callable.From(ghost.QueueFree));
        }
    }

    /// <summary>
    ///     构建板视觉（Sprite 贴图 + 碰撞）。
    /// </summary>
    private void BuildVisual()
    {
        _sprite = new Sprite2D
        {
            Texture = GameTextures.Paddle,
            Scale = new Vector2(0.5f, 0.5f)
        };
        AddChild(_sprite);

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(192, 24) }
        };
        AddChild(shape);

        AddToGroup("Paddle");
    }
}
