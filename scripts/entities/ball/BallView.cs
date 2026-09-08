using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.rules.ball;
using BreakOut.scripts.rules.bump;
using BreakOut.scripts.rules.common;
using BreakOut.scripts.entities.brick;
using BreakOut.scripts.entities.game;
using BreakOut.scripts.entities.paddle;

namespace BreakOut.scripts.entities.ball;

/// <summary>
///     球实体（薄壳）：加载 ball 场景骨架，把碰撞结果转发给规则后回写速度与表现。
/// </summary>
/// <remarks>
///     挂载于 scenes/ball/ball.tscn 根（CharacterBody2D）。碰撞分派：碰板 → 细分反弹调规则 BallMotion；碰砖 → 转发。
///     按 partial 拆分：.cs 核心 / .Dependencies 注入 / .Properties 字段 / .Events 事件 / .Signals 信号。
/// </remarks>
[Log]
[ContextAware]
public partial class BallView : CharacterBody2D
{
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
}
