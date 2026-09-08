using Godot;
using BreakOut.scripts.entities.ball;

namespace BreakOut.scripts.entities.paddle;

/// <summary>
///     板实体属性：移动/冲刺/振荡器参数与状态。
/// </summary>
public partial class PaddleView
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
    ///     冲刺速度。
    /// </summary>
    private const float DashSpeed = 1000f;

    /// <summary>
    ///     冲刺时长（秒）。
    /// </summary>
    private const float DashDuration = 0.1f;

    private const float Spring = 150f;
    private const float Damp = 10f;
    private const float VelocityMultiplier = 2f;

    private bool _dashing;
    private bool _dashCooldownReady = true;
    private bool _magnetCooldownReady = true;
    private bool _visualReady;

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
    ///     结束冲刺。
    /// </summary>
    private void EndDash()
    {
        _dashing = false;
    }

    /// <summary>
    ///     冲刺残影（原版 GhostSpawner 节奏：冲刺期间周期性克隆）。
    /// </summary>
    private void StartGhostSpawning()
    {
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
}
