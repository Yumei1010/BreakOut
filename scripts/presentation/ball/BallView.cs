using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.ball;
using BreakOut.scripts.domain.bump;
using BreakOut.scripts.domain.common;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.game;
using BreakOut.scripts.presentation.paddle;

namespace BreakOut.scripts.presentation.ball;

/// <summary>
///     球视图（薄壳）：加载 ball_layout 场景骨架，把碰撞结果转发给 domain 规则后回写速度与表现。
/// </summary>
/// <remarks>
///     挂载于 scenes/ball/ball_layout.tscn 根（CharacterBody2D），子节点（Sprite/粒子/拖尾/动画/音效）由布局场景提供。
///     碰撞分派：碰板 → 顶部/侧面 × 移动/静止细分调 domain BallMotion；碰砖 → BrickField 判定。
/// </remarks>
[Log]
[ContextAware]
public partial class BallView : CharacterBody2D
{
    private const float Radius = 12f;

    private GameRoot _root = null!;
    private PaddleView _paddle = null!;
    private Sprite2D _sprite = null!;
    private AnimationPlayer _anim = null!;
    private GpuParticles2D _speedParticles = null!;
    private GpuParticles2D _appearParticles = null!;
    private Line2D _velocityLine = null!;
    private bool _lastCollisionFacing;
    private bool _attached;
    private float _boostFactor = BumpJudge.NoBoost;
    private int _framesSincePaddleCollision;
    private int _hitstopFrames;
    private bool _visualReady;

    /// <summary>
    ///     获取或设置是否已死亡（掉出底部）。
    /// </summary>
    public bool Dead { get; set; }

    /// <summary>
    ///     获取或设置是否可移动（false 时冻结）。
    /// </summary>
    public bool CanMove { get; set; } = true;

    /// <summary>
    ///     获取自出生以来的反弹次数（供结算统计）。
    /// </summary>
    public int Bounces { get; private set; }

    /// <summary>
    ///     初始化视图：引用场景子节点（吸附由 GameRoot 组装完成后显式编排）。
    /// </summary>
    public override void _Ready()
    {
        _root = FindGameRoot();
        if (_root == null)
        {
            GD.PushError("BallView 未找到 GameRoot");
            return;
        }

        ResolveVisualNodes();
    }

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

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (!_visualReady || Dead)
        {
            return;
        }

        ScaleByVelocity();
        ColorByVelocity();
    }

    /// <summary>
    ///     随速度拉伸/朝向（原版 scale_based_on_velocity：动画播放时不覆盖）。
    /// </summary>
    private void ScaleByVelocity()
    {
        if (_anim != null && _anim.IsPlaying())
        {
            return; // bounce/appear 动画期间保持动画姿态
        }

        var speedRatio = Mathf.Clamp(Velocity.Length() / BallMotion.MaxSpeed, 0f, 1f);
        // 基准 0.375 缩放到 max 时 1.4×0.375 / 0.5×0.375
        _sprite.Scale = new Vector2(
            Mathf.Lerp(0.375f, 0.375f * 1.4f, speedRatio),
            Mathf.Lerp(0.375f, 0.375f * 0.5f, speedRatio));
        _sprite.Rotation = Velocity.Angle();
    }

    /// <summary>
    ///     随速度变色（原版 color_based_on_velocity：球/拖尾/速度粒子同步）。
    /// </summary>
    private void ColorByVelocity()
    {
        // val = remap(speed, Speed→MaxSpeed, 0→1) 钳制
        var val = Mathf.Clamp(
            (Velocity.Length() - BallMotion.Speed) / (BallMotion.MaxSpeed - BallMotion.Speed),
            0f, 1f);
        // 高速时变红（原版 max_speed_color = (1, 0, 0.2, 1)）
        var tint = Colors.White.Lerp(new Color(1f, 0f, 0.2f), val);
        _sprite.SelfModulate = tint;

        var trail = GetNodeOrNull<Line2D>("Trail2D");
        if (trail != null)
        {
            trail.DefaultColor = tint;
        }

        if (_speedParticles != null)
        {
            _speedParticles.SelfModulate = tint;
            _speedParticles.Emitting = Velocity.Length() > BallMotion.Speed + 100f;
        }
    }

    /// <summary>
    ///     每物理帧：hitstop → 吸附跟随 → 运动与碰撞。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_paddle == null || Dead || !CanMove)
        {
            return; // OnSceneReady 前冻结
        }

        if (_hitstopFrames > 0)
        {
            _hitstopFrames -= 1;
            return;
        }

        _framesSincePaddleCollision += 1;

        if (_attached)
        {
            GlobalPosition = _paddle.LaunchPoint.GlobalPosition;
            return;
        }

        var velocityBeforeCollision = Velocity;
        var collision = MoveAndCollide(Velocity * (float)delta);
        if (collision == null)
        {
            return;
        }

        Bounces += 1;

        switch (collision.GetCollider())
        {
            case PaddleView paddle:
                HandlePaddleCollision(collision, paddle);
                break;
            case BrickView brick:
                HandleBrickCollision(brick, collision.GetNormal(), velocityBeforeCollision);
                break;
            default:
                // 墙/其他（原版 HIT OTHER）：软碰撞反馈 + 标准反射
                HandleWallCollision(collision);
                break;
        }
    }

    /// <summary>
    ///     处理墙等普通碰撞（原版 HIT OTHER：软震 + 粒子 + 标准 bounce）。
    /// </summary>
    private void HandleWallCollision(KinematicCollision2D collision)
    {
        var normal = collision.GetNormal();
        Velocity = Velocity.Bounce(normal);
        _root.Sfx.PlaySoftHit();
        _root.Shake.Shake(0.15f, 20f, 5f);
        _root.PatternBounce(0.1f);
        _root.SpawnParticle("res://scenes/ball/bounce_particles.tscn",
            collision.GetPosition(), Mathf.RadToDeg(normal.Angle()));
        PlayBounceFx(normal);
    }

    /// <summary>
    ///     处理与板的碰撞：按碰撞面细分反弹。
    /// </summary>
    private void HandlePaddleCollision(KinematicCollision2D collision, PaddleView paddle)
    {
        _framesSincePaddleCollision = 0;
        paddle.PlayBounce();
        _root.Sfx.PlayPaddleBounce();
        _root.Shake.Shake(0.3f, 20f, 15f);

        var normal = collision.GetNormal();
        PlayBounceFx(normal);
        _root.PatternBounce(0.3f);
        _root.SpawnParticle("res://scenes/ball/bump_particles.tscn",
            collision.GetPosition(), Mathf.RadToDeg(normal.Angle()));

        if (normal.Dot(Vector2.Up) > 0f)
        {
            var offsetX = collision.GetPosition().X - paddle.GlobalPosition.X;
            if (paddle.Velocity.Length() > 0f)
            {
                Velocity = ToGodot(BallMotion.BounceOffMovingPaddle(
                    ToVec(Velocity), ToVec(paddle.Velocity), _boostFactor));
            }
            else
            {
                Velocity = ToGodot(BallMotion.BounceOffStaticPaddle(
                    ToVec(Velocity), offsetX, _boostFactor));
            }
        }
        else
        {
            Velocity = Velocity.Bounce(normal);
            Velocity = ToGodot(ToVec(Velocity) * (BumpJudge.NoBoost + BallMotion.BounceBaseBoost));
        }

        _boostFactor = BumpJudge.NoBoost;
        Velocity = ToGodot(BallMotion.ClampToMax(ToVec(Velocity)));
    }

    /// <summary>
    ///     处理与砖的碰撞。
    /// </summary>
    private void HandleBrickCollision(BrickView brick, Vector2 normal, Vector2 velocityBeforeCollision)
    {
        PlayBounceFx(normal);

        if (brick.IsEnergyOrExplosive)
        {
            Velocity = velocityBeforeCollision;
            _root.Sfx.PlayStrongHit();
            _root.Shake.Shake(1.0f, 25f, 20f);
            _root.PatternBounce(0.8f);
        }
        else
        {
            Velocity = Velocity.Bounce(normal);
            _root.Sfx.PlayBrickHit();
            _root.Shake.Shake(0.25f, 20f, 15f);
            _root.PatternBounce(0.25f);
            _root.SpawnParticle("res://scenes/ball/bounce_particles.tscn",
                GlobalPosition, Mathf.RadToDeg(normal.Angle()));
        }

        brick.OnBallHit();
        Velocity = ToGodot(BallMotion.ClampToMax(ToVec(Velocity)));
    }

    /// <summary>
    ///     碰撞表现：sprite 朝向法线 + 播放 bounce 动画（原版行为）。
    /// </summary>
    /// <param name="normal">碰撞法线。</param>
    private void PlayBounceFx(Vector2 normal)
    {
        if (!_visualReady)
        {
            return;
        }

        _sprite.Rotation = -normal.Angle();
        _anim?.Play("bounce");
    }

    /// <summary>
    ///     尝试 bump 击球增益。
    /// </summary>
    public void TryBump(PaddleView paddle)
    {
        var distance = GlobalPosition.DistanceTo(paddle.GlobalPosition) - 96f;
        var grade = BumpJudge.Judge(_framesSincePaddleCollision, distance);
        _boostFactor = BumpJudge.BoostOf(grade);
        StartHitstop(grade == BumpGrade.Perfect ? 10 : 5);
        _root.OnBumpJudged(grade);
        _root.SpawnBumpTiming(grade, GlobalPosition + new Vector2(0, -40));
    }

    /// <summary>
    ///     发球。
    /// </summary>
    public void Launch()
    {
        _attached = false;
        Velocity = new Vector2(0, -BallMotion.Speed);
        _boostFactor = BumpJudge.NoBoost;
        PlayAppear();
    }

    /// <summary>
    ///     吸附到板发射点。
    /// </summary>
    public void AttachToPaddle()
    {
        _attached = true;
        _paddle.AttachedBall = this;
        Velocity = Vector2.Zero;
        GlobalPosition = _paddle.LaunchPoint.GlobalPosition;
    }

    /// <summary>
    ///     球死亡。
    /// </summary>
    public void Die()
    {
        Dead = true;
        CanMove = false;
    }

    /// <summary>
    ///     进入定帧暂停。
    /// </summary>
    /// <param name="frames">冻结帧数。</param>
    public void StartHitstop(int frames)
    {
        _hitstopFrames = frames;
    }

    /// <summary>
    ///     出现粒子表现（发球/重生时）。
    /// </summary>
    /// <summary>
    ///     出现动画序列：RESET → appear（原版 await 编排），并触发出现粒子。
    /// </summary>
    public async void PlayAppear()
    {
        _appearParticles?.Restart();

        if (_anim == null)
        {
            return;
        }

        _anim.Play("RESET");
        await ToSignal(_anim, AnimationPlayer.SignalName.AnimationFinished);
        _anim.Play("appear");
    }

    private static Vec2 ToVec(Vector2 v) => new(v.X, v.Y);

    private static Vector2 ToGodot(Vec2 v) => new(v.X, v.Y);

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
