using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.ability;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.paddle;

/// <summary>
///     板视图（薄壳）：加载 paddle_layout 场景骨架，输入与能力触发转发给 domain。
/// </summary>
/// <remarks>
///     挂载于 scenes/paddle/paddle_layout.tscn 根（CharacterBody2D），子节点（Sprite/碰撞/LaunchPoint/音效/幽灵）由布局提供。
///     能力判定走 domain AbilityRule；LaunchPoint 供球吸附。
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
    private bool _visualReady;

    /// <summary>
    ///     获取发射点（球吸附位置）。
    /// </summary>
    public Marker2D LaunchPoint { get; private set; } = null!;

    /// <summary>
    ///     获取或设置当前吸附的球（null 表示无吸附）。
    /// </summary>
    public BallView? AttachedBall { get; set; }

    /// <summary>
    ///     初始化：引用布局子节点。
    /// </summary>
    public override void _Ready()
    {
        _root = GetParent<GameRoot>();
        ResolveVisualNodes();
    }

    private void ResolveVisualNodes()
    {
        LaunchPoint = GetNodeOrNull<Marker2D>("LaunchPoint") ?? new Marker2D();
        if (GetNodeOrNull<Marker2D>("LaunchPoint") == null)
        {
            LaunchPoint.Position = new Vector2(0, -30);
            AddChild(LaunchPoint);
        }

        _visualReady = GetNodeOrNull<Sprite2D>("Paddle") != null;
    }

    /// <summary>
    ///     每帧处理输入：移动与能力键。
    /// </summary>
    public override void _Process(double delta)
    {
        if (GetTree().Paused || !_visualReady)
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
    ///     每物理帧应用移动（CharacterBody2D 必须 MoveAndSlide 才产生位移）。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !_visualReady)
        {
            return;
        }

        MoveAndSlide();
    }

    /// <summary>
    ///     球碰板时的弹跳表现。
    /// </summary>
    public void PlayBounce()
    {
        GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("bounce");
    }

    /// <summary>
    ///     冲刺残影：克隆板贴图逐帧落后并淡出。
    /// </summary>
    private void SpawnGhosts()
    {
        var sprite = GetNodeOrNull<Sprite2D>("Paddle");
        if (sprite == null)
        {
            return;
        }

        for (var i = 0; i < 3; i++)
        {
            var delay = i * 0.04;
            var ghost = new Sprite2D
            {
                Texture = sprite.Texture,
                Scale = sprite.Scale,
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
}
