using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.ability;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.paddle;

/// <summary>
///     板视图（薄壳）：加载 paddle_layout 场景骨架，输入/动效转发，规则判定走 domain。
/// </summary>
/// <remarks>
///     表现层还原原版 paddle.gd 手感：
///     <list type="bullet">
///         <item>平滑加速：velocity 以 accel=20 收敛到目标速度，松手以 deccel=10 减速</item>
///         <item>弹簧振荡器：spring=150/damp=10/velocity_multiplier=2 → sprite.rotation 随移动弹性摆动</item>
///         <item>bounce 动画：球碰板触发（bulge shader + 压扁回弹）</item>
///         <item>bump 动画：按 bump 键板上弹</item>
///     </list>
/// </remarks>
[Log]
[ContextAware]
public partial class PaddleView : CharacterBody2D
{
    /// <summary>
    ///     目标移动速度。
    /// </summary>
    private const float Speed = 400f;

    /// <summary>
    ///     加速收敛率。
    /// </summary>
    private const float Accel = 20f;

    /// <summary>
    ///     减速收敛率。
    /// </summary>
    private const float Decel = 10f;

    /// <summary>
    ///     冲刺速度与时长。
    /// </summary>
    private const float DashSpeed = 1000f;

    /// <summary>
    ///     冲刺时长（秒）。
    /// </summary>
    private const float DashDuration = 0.1f;

    // ---- 弹簧振荡器参数（原版 paddle.gd） ----
    private const float Spring = 150f;
    private const float Damp = 10f;
    private const float VelocityMultiplier = 2f;

    private GameRoot _root = null!;
    private AnimationPlayer _anim = null!;
    private Sprite2D _sprite = null!;
    private bool _dashing;
    private bool _dashCooldownReady = true;
    private bool _magnetCooldownReady = true;
    private LaserView? _laser;
    private bool _visualReady;

    // ---- 振荡器状态 ----
    private float _displacement;
    private float _oscillatorVelocity;

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
        _root = FindGameRoot();
        if (_root == null)
        {
            GD.PushError("PaddleView 未找到 GameRoot");
            return;
        }

        ResolveVisualNodes();
    }

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
    ///     每帧：平滑移动 + 弹簧振荡 + 能力输入。
    /// </summary>
    public override void _Process(double delta)
    {
        if (GetTree().Paused || !_visualReady)
        {
            return;
        }

        var dt = (float)delta;
        var dir = Input.GetActionStrength("right") - Input.GetActionStrength("left");

        if (_dashing)
        {
            return; // 冲刺期间不响应常规输入
        }

        // 平滑加速/减速（原版 lerp 收敛）
        if (dir != 0)
        {
            Velocity = new Vector2(Mathf.Lerp(Velocity.X, dir * Speed, Accel * dt), 0);
        }
        else
        {
            Velocity = new Vector2(Mathf.Lerp(Velocity.X, 0f, Decel * dt), 0);
        }

        // 弹簧振荡器：速度驱动板体摆动
        _oscillatorVelocity += (Velocity.X / Speed) * VelocityMultiplier;
        var force = -Spring * _displacement + Damp * _oscillatorVelocity;
        _oscillatorVelocity -= force * dt;
        _displacement -= _oscillatorVelocity * dt;
        if (_sprite != null)
        {
            _sprite.Rotation = -_displacement;
        }

        HandleAbilityInput();
    }

    /// <summary>
    ///     处理 bump/dash/special/attract 能力键。
    /// </summary>
    private void HandleAbilityInput()
    {
        if (Input.IsActionJustPressed("bump"))
        {
            PlayBumpAnimation();
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

        if (Input.IsActionJustPressed("dash") && !_dashing && _dashCooldownReady)
        {
            if (AbilityRule.TryDash(true) == AbilityResult.Success)
            {
                _dashing = true;
                _dashCooldownReady = false;
                Velocity = new Vector2(Mathf.Sign(Velocity.X == 0 ? 1 : Velocity.X) * DashSpeed, 0);
                StartGhostSpawning();
                GetTree().CreateTimer(DashDuration).Timeout += EndDash;
                GetTree().CreateTimer(1.0).Timeout += () => _dashCooldownReady = true;
            }
        }

        if (Input.IsActionJustPressed("special"))
        {
            if (AbilityRule.TryLaser(_root.Run) == AbilityResult.Success)
            {
                _root.Run.SpendEnergy(RunState.MaxEnergy);
                _laser?.Shoot();
                _log.Debug("激光发射");
            }
        }

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

    private void EndDash()
    {
        _dashing = false;
    }

    /// <summary>
    ///     冲刺残影（原版 GhostSpawner 节奏：冲刺期间周期性克隆）。
    /// </summary>
    private void StartGhostSpawning()
    {
        // 冲刺 0.1s 内产生 3 枚残影
        for (var i = 0; i < 3; i++)
        {
            var delay = i * 0.03;
            var ghost = new Sprite2D
            {
                Texture = _sprite?.Texture,
                Scale = _sprite?.Scale ?? Vector2.One,
                GlobalPosition = GlobalPosition,
                Rotation = Rotation,
                Modulate = new Color(0f, 0.784f, 1f, 0.42f)
            };
            GetParent().AddChild(ghost);

            var tween = CreateTween();
            tween.TweenInterval(delay);
            tween.TweenProperty(ghost, "modulate:a", 0f, 0.3);
            tween.TweenCallback(Callable.From(ghost.QueueFree));
        }
    }

    /// <summary>
    ///     每物理帧应用移动。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !_visualReady)
        {
            return;
        }

        // 仅在有速度时才做物理移动（避免 MoveAndSlide 将板从墙碰撞体内推出）
        if (Velocity.LengthSquared() > 0.001f)
        {
            MoveAndSlide();
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }

    /// <summary>
    ///     球碰板：触发 bounce 动画（bulge 变形 + 压扁回弹）。
    /// </summary>
    public void PlayBounce()
    {
        _anim?.Play("bounce");
    }

    /// <summary>
    ///     玩家按 bump：触发 bump 动画（板上弹）。
    /// </summary>
    public void PlayBumpAnimation()
    {
        _anim?.Stop();
        _anim?.Play("bump");
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
