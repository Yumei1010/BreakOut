using Godot;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.effect;

namespace BreakOut.scripts.system.effect;

/// <summary>
///     特效系统：统一管理音效/相机震动/粒子爆发/相机序列（纯表现协调）。
/// </summary>
/// <remarks>
///     作为 GameRoot 子节点挂载，集中所有 juice 触发；视图通过它播特效，不再散点调用。
/// </remarks>
public partial class EffectSystem : Node
{
    private SfxManager _sfx = null!;
    private CameraShake _shake = null!;
    private Camera2D? _camera;
    private readonly Godot.Collections.Dictionary<string, PackedScene> _particleCache = new();

    /// <summary>
    ///     获取音效管理器。
    /// </summary>
    public SfxManager Sfx => _sfx;

    /// <summary>
    ///     获取相机抖动控制器。
    /// </summary>
    public CameraShake Shake => _shake;

    /// <summary>
    ///     初始化：创建音效池与相机抖动，挂到 GameRoot。
    /// </summary>
    /// <param name="camera">主相机。</param>
    public void Setup(Camera2D camera)
    {
        _camera = camera;
        _sfx = new SfxManager { Name = "Sfx" };
        AddChild(_sfx);

        _shake = new CameraShake { Name = "Shake" };
        camera.AddChild(_shake);
    }

    /// <summary>
    ///     触发相机震动。
    /// </summary>
    public void ShakeCamera(float duration, float frequency, float amplitude)
    {
        _shake.Shake(duration, frequency, amplitude);
    }

    /// <summary>
    ///     播放音效（预加载池）。
    /// </summary>
    public void PlaySound(SfxKind kind)
    {
        switch (kind)
        {
            case SfxKind.PaddleBounce: _sfx.PlayPaddleBounce(); break;
            case SfxKind.BrickHit: _sfx.PlayBrickHit(); break;
            case SfxKind.StrongHit: _sfx.PlayStrongHit(); break;
            case SfxKind.SoftHit: _sfx.PlaySoftHit(); break;
            case SfxKind.BallDestroyed: _sfx.PlayBallDestroyed(); break;
            case SfxKind.BrickDestroyed: _sfx.PlayBrickDestroyed(); break;
            case SfxKind.Explosion: _sfx.PlayExplosion(); break;
            case SfxKind.BigExplosion: _sfx.PlayBigExplosion(); break;
            case SfxKind.UltimateReady: _sfx.PlayUltimateReady(); break;
        }
    }

    /// <summary>
    ///     在位置触发一次粒子爆发（预加载缓存）。
    /// </summary>
    public void Burst(string scenePath, Vector2 position, float rotationDegrees = 0f)
    {
        if (!_particleCache.TryGetValue(scenePath, out var packed))
        {
            packed = GD.Load<PackedScene>(scenePath);
            _particleCache[scenePath] = packed;
        }

        if (packed == null)
        {
            return;
        }

        var particles = packed.Instantiate<GpuParticles2D>();
        GetParent().AddChild(particles);
        particles.GlobalPosition = position;
        particles.RotationDegrees = rotationDegrees;
        particles.Emitting = true;
        particles.Finished += particles.QueueFree;
    }

    /// <summary>
    ///     全屏 Pattern 脉冲（size_scale 弹性缩放回 30）。
    /// </summary>
    /// <param name="pattern">Pattern 节点。</param>
    /// <param name="force">冲击强度 0-1。</param>
    public void PatternBounce(ColorRect pattern, float force)
    {
        if (pattern?.Material is not ShaderMaterial material)
        {
            return;
        }

        var target = Mathf.Lerp(22.5f, 27.5f, 1f - force);
        var tween = CreateTween();
        tween.TweenProperty(material, "shader_parameter/size_scale", target, 0.15)
            .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(material, "shader_parameter/size_scale", 30f, 0.3)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    ///     从缓存取场景（供弹层等使用）。
    /// </summary>
    public PackedScene GetScene(string path)
    {
        if (!_particleCache.TryGetValue(path, out var packed))
        {
            packed = GD.Load<PackedScene>(path);
            _particleCache[path] = packed;
        }

        return packed;
    }
}

/// <summary>
///     音效类型。
/// </summary>
public enum SfxKind
{
    /// <summary>
    ///     球碰板。
    /// </summary>
    PaddleBounce,

    /// <summary>
    ///     球碰普通砖。
    /// </summary>
    BrickHit,

    /// <summary>
    ///     球碰能量/爆炸砖。
    /// </summary>
    StrongHit,

    /// <summary>
    ///     球碰墙等弱碰撞。
    /// </summary>
    SoftHit,

    /// <summary>
    ///     球摧毁（落底）。
    /// </summary>
    BallDestroyed,

    /// <summary>
    ///     砖摧毁。
    /// </summary>
    BrickDestroyed,

    /// <summary>
    ///     爆炸。
    /// </summary>
    Explosion,

    /// <summary>
    ///     强烈爆炸。
    /// </summary>
    BigExplosion,

    /// <summary>
    ///     终极就绪。
    /// </summary>
    UltimateReady
}
