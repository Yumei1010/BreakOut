using Godot;

namespace BreakOut.scripts.presentation.assets;

/// <summary>
///     游戏音效管理器：集中加载 sfxr 资产并提供池化播放。
/// </summary>
/// <remarks>
///     挂载于 GameRoot 下；所有视图通过 <c>_root.Sfx.Play(SfxKey.Xxx)</c> 触发，
///     不直接持有 AudioStreamPlayer 节点（避免单个音效节点抢占导致丢声）。
/// </remarks>
[GlobalClass]
public partial class SfxManager : Node
{
    private const int PoolSize = 8;

    private readonly Godot.Collections.Array<AudioStreamPlayer> _pool = new();
    private int _cursor;
    private AudioStreamWav? _bump;
    private AudioStreamWav? _bumpStrong;
    private AudioStreamWav? _bumpOthers;
    private AudioStreamWav? _ballDestroyed;
    private AudioStreamWav? _brickDestroyed;
    private AudioStreamWav? _explosion;
    private AudioStreamWav? _explosion2;
    private AudioStreamWav? _paddleBump;
    private AudioStreamWav? _ultimate;

    /// <summary>
    ///     初始化：加载全部音效并创建播放池。
    /// </summary>
    public override void _Ready()
    {
        _bump = Load("res://scenes/ball/audio/bump.sfxr");
        _bumpStrong = Load("res://scenes/ball/audio/bump_strong.sfxr");
        _bumpOthers = Load("res://scenes/ball/audio/bump_others.sfxr");
        _ballDestroyed = Load("res://scenes/ball/audio/ball_destroyed.sfxr");
        _brickDestroyed = Load("res://scenes/brick/audio/destroyed.sfxr");
        _explosion = Load("res://scenes/brick/audio/explosion.sfxr");
        _explosion2 = Load("res://scenes/brick/audio/explosion_2.sfxr");
        _paddleBump = Load("res://scenes/paddle/audio/paddle_bump.sfxr");
        _ultimate = Load("res://scenes/ui/ultimate/audio/ultimate.sfxr");

        for (var i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer { Name = $"Sfx{i}" };
            AddChild(player);
            _pool.Add(player);
        }
    }

    /// <summary>
    ///     播放球碰板音效。
    /// </summary>
    public void PlayPaddleBounce() => Play(_paddleBump);

    /// <summary>
    ///     播放球碰普通砖音效。
    /// </summary>
    public void PlayBrickHit() => Play(_bump);

    /// <summary>
    ///     播放球碰能量/爆炸砖音效。
    /// </summary>
    public void PlayStrongHit() => Play(_bumpStrong);

    /// <summary>
    ///     播放球碰墙等弱碰撞音效。
    /// </summary>
    public void PlaySoftHit() => Play(_bumpOthers);

    /// <summary>
    ///     播放球摧毁音效（落底）。
    /// </summary>
    public void PlayBallDestroyed() => Play(_ballDestroyed);

    /// <summary>
    ///     播放砖摧毁音效。
    /// </summary>
    public void PlayBrickDestroyed() => Play(_brickDestroyed);

    /// <summary>
    ///     播放爆炸音效。
    /// </summary>
    public void PlayExplosion() => Play(_explosion);

    /// <summary>
    ///     播放强烈爆炸音效。
    /// </summary>
    public void PlayBigExplosion() => Play(_explosion2);

    /// <summary>
    ///     播放终极就绪音效。
    /// </summary>
    public void PlayUltimateReady() => Play(_ultimate);

    private static AudioStreamWav? Load(string path)
    {
        return GD.Load<AudioStreamWav>(path);
    }

    private void Play(AudioStreamWav? stream)
    {
        if (stream == null)
        {
            return;
        }

        var player = _pool[_cursor];
        _cursor = (_cursor + 1) % _pool.Count;
        player.Stream = stream;
        player.Play();
    }
}
