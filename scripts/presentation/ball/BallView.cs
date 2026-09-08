using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.ball;
using BreakOut.scripts.domain.bump;
using BreakOut.scripts.domain.common;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.game;
using BreakOut.scripts.presentation.paddle;

namespace BreakOut.scripts.presentation.ball;

/// <summary>
///     球视图（薄壳）：Godot 物理载体，把碰撞结果转发给 domain 规则后回写速度与表现。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 ball.gd 碰撞分派：
///     碰板 → 顶部/侧面 × 移动/静止 细分后调 domain BallMotion 计算反弹速度；
///     碰砖 → 转发 BrickField 判定并触发得分事件。
///     速度/增益/角度等全部数值出自 domain 常量，本类不写魔法数。
/// </remarks>
[Log]
[ContextAware]
public partial class BallView : CharacterBody2D
{
    private const float Radius = 12f;

    private GameRoot _root = null!;
    private PaddleView _paddle = null!;
    private Sprite2D _sprite = null!;
    private bool _attached;
    private float _boostFactor = BumpJudge.NoBoost;
    private int _framesSincePaddleCollision;
    private int _hitstopFrames;

    /// <summary>
    ///     获取或设置是否已死亡（掉出底部）。
    /// </summary>
    public bool Dead { get; set; }

    /// <summary>
    ///     获取或设置是否可移动（false 时冻结，如过关/暂停）。
    /// </summary>
    public bool CanMove { get; set; } = true;

    /// <summary>
    ///     获取自出生以来的反弹次数（供结算统计）。
    /// </summary>
    public int Bounces { get; private set; }

    /// <summary>
    ///     初始化视图：取引用并吸附到板。
    /// </summary>
    public override void _Ready()
    {
        _root = GetParent<GameRoot>();
        _paddle = _root.Paddle!;
        BuildVisual();
        AttachToPaddle();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // 速度反馈：随速度水平拉伸/变色（原版 scale/color_based_on_velocity）
        if (_sprite == null || Dead)
        {
            return;
        }

        var speed = Velocity.Length();
        var t = Mathf.Clamp((speed - BallMotion.Speed) / (BallMotion.MaxSpeed - BallMotion.Speed), 0f, 1f);
        _sprite.Scale = new Vector2(0.35f + t * 0.15f, 0.35f - t * 0.08f);
        var tint = new Color(1f, 1f - t * 0.5f, 1f - t * 0.7f);
        _sprite.SelfModulate = tint;
        _sprite.Rotation = Velocity.Angle();
    }

    /// <summary>
    ///     每物理帧：hitstop → 吸附跟随 → 运动与碰撞。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Dead || !CanMove)
        {
            return;
        }

        // Hitstop：冻结若干物理帧
        if (_hitstopFrames > 0)
        {
            _hitstopFrames -= 1;
            return;
        }

        _framesSincePaddleCollision += 1;

        // 速度向基准收敛（domain 数学）
        Velocity = ToGodot(BallMotion.DecayToward(ToVec(Velocity), BallMotion.Speed, (float)delta));

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
        }
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

        // 顶部碰撞（法线朝上）最常见
        if (normal.Dot(Vector2.Up) > 0f)
        {
            var offsetX = collision.GetPosition().X - paddle.GlobalPosition.X;
            if (paddle.Velocity.Length() > 0f)
            {
                // 板在移动：吸收板速并翻转
                Velocity = ToGodot(BallMotion.BounceOffMovingPaddle(
                    ToVec(Velocity), ToVec(paddle.Velocity), _boostFactor));
            }
            else
            {
                // 板静止：按碰撞点偏移倾斜法线
                Velocity = ToGodot(BallMotion.BounceOffStaticPaddle(
                    ToVec(Velocity), offsetX, _boostFactor));
            }
        }
        else
        {
            // 侧面碰撞：保底垂直反弹
            Velocity = Velocity.Bounce(normal);
            Velocity = ToGodot(ToVec(Velocity) * (BumpJudge.NoBoost + BallMotion.BounceBaseBoost));
        }

        _boostFactor = BumpJudge.NoBoost;
        Velocity = ToGodot(BallMotion.ClampToMax(ToVec(Velocity)));
    }

    /// <summary>
    ///     处理与砖的碰撞：转发 domain 判定并触发得分。
    /// </summary>
    private void HandleBrickCollision(BrickView brick, Vector2 normal, Vector2 velocityBeforeCollision)
    {
        if (brick.IsEnergyOrExplosive)
        {
            // 能量/爆炸砖：不反弹直接穿过（保留原版手感），强反馈
            Velocity = velocityBeforeCollision;
            _root.Sfx.PlayStrongHit();
            _root.Shake.Shake(1.0f, 25f, 20f);
        }
        else
        {
            Velocity = Velocity.Bounce(normal);
            _root.Sfx.PlayBrickHit();
            _root.Shake.Shake(0.25f, 20f, 15f);
        }

        brick.OnBallHit();
        Velocity = ToGodot(BallMotion.ClampToMax(ToVec(Velocity)));
    }

    /// <summary>
    ///     尝试 bump 击球增益（玩家在球离板后按 bump 提升）。
    /// </summary>
    /// <param name="paddle">板视图。</param>
    public void TryBump(PaddleView paddle)
    {
        var distance = GlobalPosition.DistanceTo(paddle.GlobalPosition) - 96f;
        var grade = BumpJudge.Judge(_framesSincePaddleCollision, distance);
        _boostFactor = BumpJudge.BoostOf(grade);
        StartHitstop(grade == BumpGrade.Perfect ? 10 : 5);
        _root.OnBumpJudged(grade);
    }

    /// <summary>
    ///     发球：解除吸附，赋予向上初速。
    /// </summary>
    public void Launch()
    {
        _attached = false;
        Velocity = new Vector2(0, -BallMotion.Speed);
        _boostFactor = BumpJudge.NoBoost;
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
    ///     球死亡（掉出底部）。
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
    ///     构建视觉与碰撞（球纹理 + 圆形碰撞）。
    /// </summary>
    private void BuildVisual()
    {
        _sprite = new Sprite2D
        {
            Texture = GameTextures.Ball,
            Scale = new Vector2(0.35f, 0.35f)
        };
        AddChild(_sprite);

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = Radius }
        };
        AddChild(shape);
        AddToGroup("Ball");
    }

    /// <inheritdoc />
    public override void _Draw()
    {
    }

    /// <summary>
    ///     转换 Godot 向量为 domain 向量。
    /// </summary>
    private static Vec2 ToVec(Vector2 v) => new(v.X, v.Y);

    /// <summary>
    ///     转换 domain 向量为 Godot 向量。
    /// </summary>
    private static Vector2 ToGodot(Vec2 v) => new(v.X, v.Y);
}
