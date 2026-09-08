using Godot;
using BreakOut.scripts.rules.ball;
using BreakOut.scripts.rules.bump;

namespace BreakOut.scripts.entities.ball;

/// <summary>
///     球实体属性：字段、状态与速度视觉。
/// </summary>
public partial class BallView
{
    /// <summary>
    ///     球碰撞半径。
    /// </summary>
    private const float Radius = 12f;

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
    ///     随速度拉伸/朝向（原版 scale_based_on_velocity：动画播放时不覆盖）。
    /// </summary>
    private void ScaleByVelocity()
    {
        if (_anim != null && _anim.IsPlaying())
        {
            return;
        }

        var speedRatio = Mathf.Clamp(Velocity.Length() / BallMotion.MaxSpeed, 0f, 1f);
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
        var val = Mathf.Clamp(
            (Velocity.Length() - BallMotion.Speed) / (BallMotion.MaxSpeed - BallMotion.Speed),
            0f, 1f);
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
}
