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
using BreakOut.scripts.domain.brick;
using BreakOut.scripts.domain.bump;
using BreakOut.scripts.domain.common;
using BreakOut.scripts.domain.level;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.domain.scoring;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.effect;
using BreakOut.scripts.presentation.paddle;
using BreakOut.scripts.presentation.ui;
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
    /// <summary>
    ///     砖墙碰撞影响半径（用于爆炸连锁）。
    /// </summary>
    private const float ExplosionRadius = 120f;

    private readonly List<BrickView> _brickViews = new();
    private Camera2D? _camera;

    /// <summary>
    ///     获取本局状态（domain）。
    /// </summary>
    public RunState Run { get; } = new();

    /// <summary>
    ///     获取计分规则（domain）。
    /// </summary>
    public ScoreRule Score { get; } = new();

    /// <summary>
    ///     获取砖墙（domain）。
    /// </summary>
    public BrickField BrickField { get; } = new(ExplosionRadius);

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
        ResolveSceneNodes();
        AttachJuice();
        GenerateLevel();
        PublishInitialState();
        _log.Info($"BreakOut 玩法就绪：{BrickField.AliveCount} 块砖");
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

        // HUD（场景无 HUD 节点时动态创建）
        if (GetNodeOrNull("HudLayer") == null)
        {
            var hudLayer = new CanvasLayer { Name = "HudLayer" };
            var hud = new HudView { Name = "Hud" };
            hudLayer.AddChild(hud);

            var feedback = new FeedbackLayer { Name = "Feedback" };
            hudLayer.AddChild(feedback);

            var overlay = new ResultOverlay { Name = "ResultOverlay" };
            hudLayer.AddChild(overlay);
            AddChild(hudLayer);
        }
    }

    /// <summary>
    ///     每物理帧检查边界（球出界）。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Ball == null || Ball.Dead || !Ball.CanMove)
        {
            return;
        }

        if (Ball.GlobalPosition.Y > GetViewportRect().Size.Y + 60f)
        {
            OnBallLost();
        }
    }

    /// <summary>
    ///     生成新关卡：读场景 SpawnPos 锚点生成砖墙。
    /// </summary>
    private void GenerateLevel()
    {
        ClearBricks();

        var anchors = ReadSpawnAnchors();
        var generator = new LevelGenerator();
        var spawns = generator.Generate(anchors, LevelConfig.Default);

        var brickScene = GD.Load<PackedScene>("res://scenes/brick/brick_layout.tscn");
        var bricksRoot = GetNodeOrNull<Node2D>("Bricks") ?? this;

        foreach (var spawn in spawns)
        {
            var data = new Brick(spawn.Type, spawn.Size);
            BrickField.Add(data, spawn.Position);

            var view = brickScene.Instantiate<BrickView>();
            view.Position = ToGodot(spawn.Position);
            bricksRoot.AddChild(view);
            view.Setup(data);
            _brickViews.Add(view);
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
        var bricksRoot = GetNodeOrNull("Bricks");
        if (bricksRoot != null)
        {
            foreach (var child in bricksRoot.GetChildren())
            {
                if (child is BrickView view)
                {
                    view.QueueFree();
                }
                else if (child is StaticBody2D)
                {
                    child.QueueFree(); // 场景预置砖
                }
            }
        }

        _brickViews.Clear();
    }

    /// <summary>
    ///     广播初始状态（HUD 首次填充）。
    /// </summary>
    private void PublishInitialState()
    {
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Score.Score, Score.Combo));
        this.SendEvent(ChannelConstants.Gameplay, new EnergyChangedEvent(Run.Energy, false));
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
        var dead = Run.OnBallLost();
        Sfx.PlayBallDestroyed();
        Shake.Shake(0.45f, 30f, 25f);
        this.SendEvent(ChannelConstants.Gameplay, new BallLostEvent(Run.Health, dead));
        _log.Debug($"球落底，剩余生命 {Run.Health}");

        if (dead)
        {
            _log.Warn("游戏结束");
            PlayDeathSequence();
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
        Run.OnBrickHit();
        Score.OnBrickTouched();
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Score.Score, Score.Combo));
        this.SendEvent(ChannelConstants.Gameplay,
            new EnergyChangedEvent(Run.Energy, Run.Energy >= RunState.MaxEnergy));
    }

    /// <summary>
    ///     砖被摧毁：计分、碎裂表现、清场判断并广播。
    /// </summary>
    public void OnBrickDestroyed(BrickView view, IReadOnlyList<BrickDestroyedResult> results)
    {
        foreach (var result in results)
        {
            Score.OnBrickDestroyed();
            _log.Debug($"砖摧毁 {result.Brick.Type}（{result.Reason}）");
        }

        foreach (var result in results)
        {
            BurstDebris(view.GlobalPosition, new Color(0.85f, 0.5f, 0.2f));
        }

        Sfx.PlayBrickDestroyed();
        if (results.Any(r => BrickSpecs.IsExplosive(r.Brick.Type)))
        {
            Sfx.PlayExplosion();
        }

        view.QueueFree();
        _brickViews.Remove(view);

        var levelCleared = BrickField.AliveCount == 0;
        this.SendEvent(ChannelConstants.Gameplay,
            new BrickDestroyedEvent(results.Count, BrickField.AliveCount, levelCleared));
        this.SendEvent(ChannelConstants.Gameplay, new ScoreChangedEvent(Score.Score, Score.Combo));

        if (levelCleared)
        {
            Run.OnLevelCleared();
            _log.Info("关卡清除！");
            PlayLevelClearSequence();
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

        this.SendEvent(ChannelConstants.Gameplay, new BumpJudgedEvent(grade));
    }

    /// <summary>
    ///     计算当前对局结算结果。
    /// </summary>
    public StageResult BuildStageResult()
    {
        return new StageResult(0, 0, 0, Ball?.Bounces ?? 0, Score.Score);
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
        await FocusBallTween(zoomTo: 1.6f);
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
        await FocusBallTween(zoomTo: 1.5f);
    }

    /// <summary>
    ///     慢动作 → 聚焦球放大 → 停顿 → 恢复回位。
    /// </summary>
    private async System.Threading.Tasks.Task FocusBallTween(float zoomTo)
    {
        if (_camera == null || Ball == null)
        {
            return;
        }

        var originalZoom = _camera.Zoom;
        var originalPos = _camera.Position;

        global::Godot.Engine.TimeScale = 0.15f;

        var focus = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        focus.Parallel().TweenProperty(_camera, "position", Ball.GlobalPosition, 0.3);
        focus.Parallel().TweenProperty(_camera, "zoom", new Vector2(zoomTo, zoomTo), 0.3);
        await ToSignal(focus, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);

        global::Godot.Engine.TimeScale = 1.0f;

        var restore = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        restore.Parallel().TweenProperty(_camera, "position", originalPos, 0.4);
        restore.Parallel().TweenProperty(_camera, "zoom", originalZoom, 0.4);
        await ToSignal(restore, Tween.SignalName.Finished);
    }

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
