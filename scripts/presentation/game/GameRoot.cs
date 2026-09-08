using System.Collections.Generic;
using System.Linq;
using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.brick.@event;
using BreakOut.scripts.cqrs.bump.@event;
using BreakOut.scripts.cqrs.run.@event;
using BreakOut.scripts.cqrs.scoring.@event;
using BreakOut.scripts.rules.brick;
using BreakOut.scripts.rules.bump;
using BreakOut.scripts.rules.common;
using BreakOut.scripts.rules.level;
using BreakOut.scripts.rules.run;
using BreakOut.scripts.rules.scoring;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.effect;
using BreakOut.scripts.presentation.paddle;
using BreakOut.scripts.presentation.ui;
using BreakOut.scripts.system.brick;
using BreakOut.scripts.system.level;
using BreakOut.scripts.system.scoring;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.game;

/// <summary>
///     玩法场景根：挂载于 main(game).tscn 根，场景驱动——从场景读取 Paddle/Ball/相机/锚点。
/// </summary>
/// <remarks>
///     场景节点树（背景/墙/死亡区/Pattern/锚点/相机）由 game.tscn 布局提供；
///     本类只做 domain 组装、砖墙生成、事件桥与相机序列。Paddle/Ball 为场景内实例。
/// </remarks>
[Log]
[ContextAware]
public partial class GameRoot : Node2D
{
    private static readonly string[] PreloadScenePaths =
    [
        "res://scenes/brick/brick.tscn",
        "res://scenes/ui/game_over/game_over.tscn",
        "res://scenes/ui/stage_clear/stage_clear.tscn",
        "res://scenes/ui/ultimate/ultimate_ready.tscn",
        "res://scenes/effects/bump/bump_timing.tscn",
        "res://scenes/ball/bounce_particles.tscn",
        "res://scenes/ball/bump_particles.tscn",
        "res://scenes/ball/ball_explode_particles.tscn",
        "res://scenes/brick/brick_explode_particles.tscn",
        "res://scenes/brick/bomb_explode_particles.tscn",
        "res://scenes/game/lava_splash_particles.tscn",
    ];


    /// <summary>
    ///     砖墙碰撞影响半径（用于爆炸连锁）。
    /// </summary>
    private const float ExplosionRadius = 120f;

    private Camera2D? _camera;
    private ColorRect? _pattern;
    private ColorRect? _bw;
    private CanvasLayer? _uiLayer;
    private readonly Dictionary<string, PackedScene> _sceneCache = new();
    private Label? _comboLabel;
    private Timer? _comboHideTimer;

    private bool _ultimateShown;

    /// <summary>
    ///     获取本局状态（domain）。
    /// </summary>
    /// <summary>
    ///     获取计分/对局系统（持有规则，纯 C#）。
    /// </summary>
    public ScoringSystem Scoring { get; } = new();

    /// <summary>
    ///     获取砖墙系统（规则 + 视图注册表）。
    /// </summary>
    public BrickSystem Bricks { get; } = new();

    /// <summary>
    ///     获取关卡系统。
    /// </summary>
    public LevelSystem Level { get; } = new();

    /// <summary>
    ///     获取音效管理器。
    /// </summary>
    public SfxManager Sfx { get; private set; } = null!;

    /// <summary>
    ///     获取相机抖动控制器。
    /// </summary>
    public CameraShake Shake { get; private set; } = null!;

    /// <summary>
    ///     获取板视图（场景实例）。
    /// </summary>
    public PaddleView? Paddle { get; private set; }

    /// <summary>
    ///     获取球视图（场景实例）。
    /// </summary>
    public BallView? Ball { get; private set; }

    /// <summary>
    ///     创建视图引用并广播初始状态。
    /// </summary>
    public override void _Ready()
    {
        global::Godot.Engine.TimeScale = 1.0f;
        PreloadScenesIntoCache();
        ResolveSceneNodes();
        AttachJuice();
        GenerateLevel();
        PublishInitialState();
        _log.Info($"BreakOut 玩法就绪：{Bricks.AliveCount} 块砖");
    }

    /// <summary>
    ///     解析场景内节点引用。
    /// </summary>
    private void ResolveSceneNodes()
    {
        Paddle = GetNodeOrNull<PaddleView>("Paddle");
        Ball = GetNodeOrNull<BallView>("Ball");
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera != null)
        {
            _camera.Position = new Vector2(960, 540);
        }

        _pattern = GetNodeOrNull<ColorRect>("Pattern");
        _bw = GetNodeOrNull<ColorRect>("EffectCanvasLayer/BW");
        _comboLabel = GetNodeOrNull<Label>("Combo");
        if (_comboLabel != null)
        {
            _comboHideTimer = new Timer { OneShot = true, WaitTime = 2.0 };
            AddChild(_comboHideTimer);
            _comboHideTimer.Timeout += () =>
            {
                var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                tween.TweenProperty(_comboLabel, "scale", Vector2.Zero, 0.3);
            };
        }

        // 组装完成后再初始化球（Paddle 已就绪）
        if (Paddle != null && Ball != null)
        {
            Ball.OnSceneReady(Paddle);
            Ball.PlayAppear(); // 开局出现动画

        }
    }

    /// <summary>
    ///     挂载音效与相机抖动（SfxManager 节点在场景外，此处动态创建）。
    /// </summary>
    private void AttachJuice()
    {
        Sfx = new SfxManager { Name = "Sfx" };
        AddChild(Sfx);

        if (_camera == null)
        {
            _camera = new Camera2D { Position = new Vector2(960, 540) };
            AddChild(_camera);
        }

        Shake = new CameraShake { Name = "Shake" };
        _camera.AddChild(Shake);

        // 弹层统一挂 HUDCanvasLayer（原版 UI 场景已在场景内）
        _uiLayer = GetNodeOrNull<CanvasLayer>("HUDCanvasLayer");
    }

    /// <summary>
    ///     每物理帧检查边界（球出界）。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        Scoring.TickTime(delta);

        if (Ball == null || Ball.Dead || !Ball.CanMove)
        {
            return;
        }

        if (Ball.GlobalPosition.Y > GetViewportRect().Size.Y + 60f)
        {
            _log.Debug($"球出界 Y={Ball.GlobalPosition.Y}");
            OnBallLost();
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        // 兜底：离开场景时恢复时间尺度（死亡慢动作可能被重载中断）
        global::Godot.Engine.TimeScale = 1.0f;
    }

    /// <summary>
    ///     生成新关卡：读场景 SpawnPos 锚点生成砖墙。
    /// </summary>
    private void GenerateLevel()
    {
        ClearBricks();

        var anchors = ReadSpawnAnchors();
        var spawns = Level.Generate(anchors, LevelConfig.Default);

        var brickScene = GetScene("res://scenes/brick/brick.tscn");
        var bricksRoot = GetNodeOrNull<Node2D>("Bricks") ?? this;

        foreach (var spawn in spawns)
        {
            Bricks.AddBrick(spawn, brickScene, bricksRoot);
        }
    }

    /// <summary>
    ///     读取场景 SpawnPos 下的 Marker2D 锚点位置。
    /// </summary>
    private IReadOnlyList<Vec2> ReadSpawnAnchors()
    {
        var anchors = new List<Vec2>();
        var spawnPos = GetNodeOrNull("SpawnPos");
        if (spawnPos != null)
        {
            foreach (var child in spawnPos.GetChildren())
            {
                if (child is Marker2D marker)
                {
                    anchors.Add(ToVec(marker.GlobalPosition));
                }
            }
        }

        // 场景无锚点时回退默认网格
        return anchors.Count > 0 ? anchors : BuildSpawnGrid();
    }

    /// <summary>
    ///     清空砖（视图 + domain）。
    /// </summary>
    private void ClearBricks()
    {
        var bricksRoot = GetNodeOrNull<Node2D>("Bricks");
        if (bricksRoot != null)
        {
            Bricks.ClearAll(bricksRoot);
        }
    }

    /// <summary>
    ///     广播初始状态（HUD 首次填充）。
    /// </summary>
    private void PublishInitialState()
    {
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Scoring.Score.Score, Scoring.Score.Combo));
        this.SendEvent(ChannelConstants.Gameplay, new EnergyChangedEvent(Scoring.Run.Energy, false));
    }

    /// <summary>
    ///     球落底：扣命、广播并处理重挂/游戏结束。
    /// </summary>
    private void OnBallLost()
    {
        if (Ball == null)
        {
            return;
        }

        Ball.Die();
        SpawnParticle("res://scenes/ball/ball_explode_particles.tscn", Ball.GlobalPosition);
        SpawnParticle("res://scenes/game/lava_splash_particles.tscn", Ball.GlobalPosition + new Vector2(0, 20));
        var dead = Scoring.Run.OnBallLost();
        Sfx.PlayBallDestroyed();
        Shake.Shake(0.45f, 30f, 25f);
        this.SendEvent(ChannelConstants.Gameplay, new BallLostEvent(Scoring.Run.Health, dead));
        _log.Debug($"球落底，剩余生命 {Scoring.Run.Health}");

        if (dead)
        {
            _log.Info("游戏结束");
            PlayDeathSequence();
            ShowGameOver();
            return;
        }

        Ball.Dead = false;
        Ball.CanMove = true;
        Ball.AttachToPaddle();
    }

    /// <summary>
    ///     砖被球碰到（未摧毁）：补能量与触碰分并广播。
    /// </summary>
    public void OnBrickHit(BrickType visualType)
    {
        Scoring.OnBrickHitEnergy();
        Scoring.OnBrickTouched();
        ShowCombo();
        TryShowUltimate();
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Scoring.Score.Score, Scoring.Score.Combo));
        this.SendEvent(ChannelConstants.Gameplay,
            new EnergyChangedEvent(Scoring.Run.Energy, Scoring.Run.Energy >= BreakOut.scripts.rules.run.RunState.MaxEnergy));
    }

    /// <summary>
    ///     能量砖被摧毁（BrickField 回调）：补能量并检查满能量提示。
    /// </summary>
    public void OnEnergyBrickDestroyed()
    {
        Scoring.OnEnergyBrickDestroyed();
        TryShowUltimate();
    }

    /// <summary>
    ///     显示 Combo 大字（combo&gt;1 时居中弹出，2s 超时缩放消失）。
    /// </summary>
    private void ShowCombo()
    {
        if (_comboLabel == null || _comboHideTimer == null)
        {
            return;
        }

        if (Scoring.Score.Combo <= 1)
        {
            _comboLabel.Visible = false;
            _comboHideTimer.Stop();
            return;
        }

        _comboLabel.Visible = true;
        _comboLabel.Text = $"COMBO {Scoring.Score.Combo}";
        _comboLabel.Scale = new Vector2(0.4f, 0.4f);
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_comboLabel, "scale", Vector2.One, 0.25);
        tween.TweenProperty(_comboLabel, "modulate:a", 1f, 0.1);

        _comboHideTimer.Start(2.0);
    }

    /// <summary>
    ///     能量刚达上限时显示终极就绪提示（能量回落后可再次触发）。
    /// </summary>
    private void TryShowUltimate()
    {
        if (Scoring.Run.Energy >= BreakOut.scripts.rules.run.RunState.MaxEnergy)
        {
            ShowUltimateReady();
        }
    }

    /// <summary>
    ///     砖被摧毁：计分、碎裂表现、清场判断并广播。
    /// </summary>
    public void OnBrickDestroyed(BrickView view, IReadOnlyList<BrickDestroyedResult> results)
    {
        foreach (var result in results)
        {
            Scoring.OnBrickDestroyed();
            _log.Debug($"砖摧毁 {result.Brick.Type}（{result.Reason}）");
        }

        SpawnParticle("res://scenes/brick/brick_explode_particles.tscn", view.GlobalPosition);
        if (results.Any(r => BrickSpecs.IsExplosive(r.Brick.Type)))
        {
            SpawnParticle("res://scenes/brick/bomb_explode_particles.tscn", view.GlobalPosition);
        }

        Sfx.PlayBrickDestroyed();
        if (results.Any(r => BrickSpecs.IsExplosive(r.Brick.Type)))
        {
            Sfx.PlayExplosion();
        }

        view.QueueFree();
        Bricks.RemoveView(view);

        var levelCleared = Bricks.AliveCount == 0;
        ShowCombo();
        this.SendEvent(ChannelConstants.Gameplay,
            new BrickDestroyedEvent(results.Count, Bricks.AliveCount, levelCleared));
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Scoring.Score.Score, Scoring.Score.Combo));

        if (levelCleared)
        {
            Scoring.Run.OnLevelCleared();
            _log.Info("关卡清除！");
            PlayLevelClearSequence();
            ShowStageClear();
        }
    }

    /// <summary>
    ///     显示游戏结束弹层（原版 game_over 场景）。
    /// </summary>
    private void ShowGameOver()
    {
        AddOverlay<GameOverView>("res://scenes/ui/game_over/game_over.tscn");
    }

    /// <summary>
    ///     显示过关结算弹层（原版 stage_clear 场景）。
    /// </summary>
    private void ShowStageClear()
    {
        AddOverlay<StageClearView>("res://scenes/ui/stage_clear/stage_clear.tscn");
    }

    /// <summary>
    ///     显示满能量提示（原版 ultimate_ready 场景）。
    /// </summary>
    public void ShowUltimateReady()
    {
        if (_uiLayer != null && _uiLayer.GetNodeOrNull("UltimateReady") == null)
        {
            var view = GetScene("res://scenes/ui/ultimate/ultimate_ready.tscn").Instantiate<UltimateReadyView>();
            _uiLayer.AddChild(view);
        }
    }

    /// <summary>
    ///     在位置显示 bump 判定飘字（原版 bump_timing 场景）。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    /// <param name="position">飘字位置（球碰撞处）。</param>
    public void SpawnBumpTiming(BumpGrade grade, Vector2 position)
    {
        if (_uiLayer == null)
        {
            return;
        }

        var packed = GetScene("res://scenes/effects/bump/bump_timing.tscn");
        var view = packed.Instantiate<BumpTimingView>();
        _uiLayer.AddChild(view);
        view.Position = position;
        view.Setup(grade);
    }

    /// <summary>
    ///     向 UI 层添加一个弹层场景实例。
    /// </summary>
    private void AddOverlay<T>(string scenePath) where T : Node
    {
        if (_uiLayer == null)
        {
            return;
        }

        var packed = GetScene(scenePath);
        var view = packed.Instantiate<T>();
        view.Name = typeof(T).Name;
        _uiLayer.AddChild(view);
        if (view is CanvasItem canvasItem)
        {
            canvasItem.MoveToFront();
        }
    }

    /// <summary>
    ///     记录 bump 判定并广播。
    /// </summary>
    public void OnBumpJudged(BumpGrade grade)
    {
        Sfx.PlaySoftHit();
        if (grade == BumpGrade.Perfect)
        {
            Sfx.PlayStrongHit();
        }

        Scoring.RecordBump(grade);

        this.SendEvent(ChannelConstants.Gameplay, new BumpJudgedEvent(grade));
    }

    /// <summary>
    ///     计算当前对局结算结果。
    /// </summary>
    public StageResult BuildStageResult()
    {
        return Scoring.BuildStageResult(Ball?.Bounces ?? 0);
    }

    /// <summary>
    ///     回退锚点网格（场景无 SpawnPos 时）。
    /// </summary>
    private static Vec2[] BuildSpawnGrid()
    {
        var positions = new List<Vec2>();
        for (var row = 0; row < 3; row++)
        {
            var y = 237 + row * 128;
            var minCol = row == 2 ? 1 : 0;
            var maxCol = row == 2 ? 7 : 8;
            for (var col = minCol; col < maxCol; col++)
            {
                positions.Add(new Vec2(259 + col * 200, y));
            }
        }

        return positions.ToArray();
    }

    private static Vec2 ToVec(Vector2 v) => new(v.X, v.Y);

    private static Vector2 ToGodot(Vec2 v) => new(v.X, v.Y);

    /// <summary>
    ///     全屏 Pattern 脉冲（原版 pattern.gd bounce：size_scale 弹性缩放后回 30）。
    /// </summary>
    /// <param name="force">冲击强度 0-1。</param>
    public void PatternBounce(float force)
    {
        if (_pattern?.Material is not ShaderMaterial material)
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

    // ==== 粒子效果（实例化 *_particles.tscn，one_shot 自停 + 延迟清理） ====

    private static readonly string[] BurstParticleScenes =
    [
        "res://scenes/ball/bounce_particles.tscn",
        "res://scenes/ball/bump_particles.tscn",
        "res://scenes/ball/ball_explode_particles.tscn",
        "res://scenes/brick/brick_explode_particles.tscn",
        "res://scenes/brick/bomb_explode_particles.tscn",
    ];

    /// <summary>
    ///     启动时预加载全部场景（碰撞粒子/弹层），避免首次触发加载卡顿。
    /// </summary>
    private void PreloadScenesIntoCache()
    {
        foreach (var path in PreloadScenePaths)
        {
            _sceneCache[path] = GD.Load<PackedScene>(path);
        }
    }

    /// <summary>
    ///     从缓存取场景（未缓存则现载）。
    /// </summary>
    private PackedScene GetScene(string path)
    {
        if (!_sceneCache.TryGetValue(path, out var packed))
        {
            packed = GD.Load<PackedScene>(path);
            _sceneCache[path] = packed;
        }

        return packed;
    }

    /// <summary>
    ///     在位置触发一个一次性粒子爆发。
    /// </summary>
    /// <param name="scenePath">粒子场景路径。</param>
    /// <param name="position">爆发位置。</param>
    /// <param name="rotationDegrees">旋转（法线角转度）。</param>
    public void SpawnParticle(string scenePath, Vector2 position, float rotationDegrees = 0f)
    {
        var packed = GetScene(scenePath);
        if (packed == null)
        {
            return;
        }

        var particles = packed.Instantiate<GpuParticles2D>();
        AddChild(particles);
        particles.GlobalPosition = position;
        particles.RotationDegrees = rotationDegrees;
        particles.Emitting = true;

        // one_shot 结束后延迟一帧清理
        particles.Finished += particles.QueueFree;
    }

    // ==== juice：相机序列 + 碎裂粒子 ====

    /// <summary>
    ///     关卡清除序列：慢动作聚焦球后回位。
    /// </summary>
    private async void PlayLevelClearSequence()
    {
        if (_camera == null || Ball == null)
        {
            return;
        }

        Sfx.PlayUltimateReady();
        Shake.Shake(0.5f, 25f, 25f);

        // 短促慢动作（镜头不动，避免出界露纹理边）
        global::Godot.Engine.TimeScale = 0.4f;
        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
        global::Godot.Engine.TimeScale = 1.0f;
    }

    /// <summary>
    ///     死亡序列：慢动作聚焦球。
    /// </summary>
    private async void PlayDeathSequence()
    {
        if (_camera == null || Ball == null)
        {
            return;
        }

        Sfx.PlayBallDestroyed();
        Shake.Shake(0.5f, 30f, 20f);
        FadeGrayscale(true);

        // 短促慢动作（镜头不动）
        global::Godot.Engine.TimeScale = 0.3f;
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        global::Godot.Engine.TimeScale = 1.0f;
        FadeGrayscale(false);
    }

    /// <summary>
    ///     BW 灰阶渐变（原版死亡序列的 mix_val 控制）。
    /// </summary>
    /// <param name="on">true 渐变到黑白。</param>
    private void FadeGrayscale(bool on)
    {
        if (_bw?.Material is not ShaderMaterial material)
        {
            return;
        }

        var tween = CreateTween();
        tween.TweenProperty(material, "shader_parameter/mix_val", on ? 1f : 0f, 0.5)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    ///     慢动作 → 聚焦球放大 → 停顿 → 恢复回位。
    /// </summary>
    /// <summary>
    ///     在位置生成一次碎裂视觉。
    /// </summary>
    public void BurstDebris(Vector2 position, Color baseColor)
    {
        for (var i = 0; i < 8; i++)
        {
            var piece = new ColorRect
            {
                Size = new Vector2(14, 14),
                Color = baseColor,
                Position = position - new Vector2(7, 7),
                Rotation = (float)GD.RandRange(0, Mathf.Tau)
            };
            AddChild(piece);

            var tween = CreateTween().SetParallel(true);
            var dir = Vector2.Right.Rotated((float)GD.RandRange(0, Mathf.Tau));
            tween.TweenProperty(piece, "position", piece.Position + dir * (float)GD.RandRange(60, 140), 0.5)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(piece, "modulate:a", 0f, 0.4)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            tween.Chain().TweenCallback(Callable.From(piece.QueueFree));
        }
    }
}
