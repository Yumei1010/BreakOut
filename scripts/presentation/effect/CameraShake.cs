using Godot;

namespace BreakOut.scripts.presentation.effect;

/// <summary>
///     相机抖动控制器：移植自 game_juice_breakout_4 的 camera_final.gd 抖动数学。
/// </summary>
/// <remarks>
///     平滑噪声衰减抖动：振幅随时间从初始值衰减到 0，每 1/频率 秒采样一次平滑噪声。
///     依赖 Godot 的 position_smoothing 由外部相机节点提供（本组件只管 offset）。
/// </remarks>
public partial class CameraShake : Node
{
    private float _duration;
    private float _periodInSeconds;
    private float _amplitude;
    private float _timer;
    private float _lastShookTimer;
    private float _previousX;
    private float _previousY;
    private Vector2 _lastOffset;
    private Camera2D? _camera;

    /// <summary>
    ///     初始化：绑定所属相机。
    /// </summary>
    public override void _Ready()
    {
        _camera = GetParent<Camera2D>();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_camera == null || _timer == 0f)
        {
            return;
        }

        var dt = (float)delta;
        _lastShookTimer += dt;

        while (_lastShookTimer >= _periodInSeconds)
        {
            _lastShookTimer -= _periodInSeconds;

            // 振幅随剩余时间衰减
            var intensity = _amplitude * (1f - (_duration - _timer) / _duration);

            // 平滑噪声采样（继承原版近似）
            var newX = (float)GD.RandRange(-1.0, 1.0);
            var xComponent = intensity * (_previousX + dt * (newX - _previousX));
            var newY = (float)GD.RandRange(-1.0, 1.0);
            var yComponent = intensity * (_previousY + dt * (newY - _previousY));
            _previousX = newX;
            _previousY = newY;

            var newOffset = new Vector2(xComponent, yComponent);
            _camera.Offset = _camera.Offset - _lastOffset + newOffset;
            _lastOffset = newOffset;
        }

        _timer -= dt;
        if (_timer <= 0f)
        {
            _timer = 0f;
            _camera.Offset = _camera.Offset - _lastOffset;
            _lastOffset = Vector2.Zero;
        }
    }

    /// <summary>
    ///     触发一次抖动。
    /// </summary>
    /// <param name="duration">持续时间（秒）。</param>
    /// <param name="frequency">采样频率（次/秒）。</param>
    /// <param name="amplitude">初始振幅（像素）。</param>
    public void Shake(float duration, float frequency, float amplitude)
    {
        if (frequency <= 0f || _camera == null)
        {
            return;
        }

        _duration = duration;
        _timer = duration;
        _periodInSeconds = 1f / frequency;
        _amplitude = amplitude;
        _previousX = (float)GD.RandRange(-1.0, 1.0);
        _previousY = (float)GD.RandRange(-1.0, 1.0);

        _camera.Offset = _camera.Offset - _lastOffset;
        _lastOffset = Vector2.Zero;
    }
}
