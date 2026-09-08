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
using BreakOut.scripts.presentation.effect;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.paddle;
using BreakOut.scripts.presentation.ui;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.game;

/// <summary>
///     玩法场景根节点：组装 domain 实例、生成关卡、驱动视图，并在规则级状态变化时广播 CQRS 事件。
/// </summary>
/// <remarks>
///     单场景游戏不走 UI 页面栈，本节点常驻 main.tscn。
///     事件桥：规则变化（得分/生命/能量/bump/砖毁）发事件 → HUD/特效订阅刷新，视图不做直接耦合。
/// </remarks>
[Log]
[ContextAware]
public partial class GameRoot : Node2D
{
    /// <summary>
    ///     砖墙碰撞影响半径（用于爆炸连锁）。
    /// </summary>
    private const float ExplosionRadius = 120f;

    private static readonly Vec2[] SpawnPositions = BuildSpawnGrid();

    private readonly List<BrickView> _brickViews = new();

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
    ///     获取板视图。
    /// </summary>
    public PaddleView? Paddle { get; private set; }

    /// <summary>
    ///     获取球视图。
    /// </summary>
    public BallView? Ball { get; private set; }

    /// <summary>
    ///     创建视图节点（场景树构建）并广播初始状态。
    /// </summary>
    public override void _Ready()
    {
        BuildScene();
        GenerateLevel();
        PublishInitialState();
        _log.Info($"BreakOut 玩法就绪：{BrickField.AliveCount} 块砖");
    }

    /// <summary>
    ///     每物理帧检查边界（球出界等）。
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
    ///     构建玩法场景节点树。
    /// </summary>
    private void BuildScene()
    {
        var background = new Sprite2D
        {
            Texture = GameTextures.Background,
            Centered = false,
            Scale = new Vector2(2f, 2f)
        };
        AddChild(background);

        // 相机 + 抖动
        var camera = new Camera2D { Position = new Vector2(960, 540) };
        AddChild(camera);
        Shake = new CameraShake { Name = "Shake" };
        camera.AddChild(Shake);

        // 音效
        Sfx = new SfxManager { Name = "Sfx" };
        AddChild(Sfx);

        Paddle = new PaddleView { Position = new Vector2(960, 990) };
        Ball = new BallView { Position = new Vector2(960, 900) };
        AddChild(Paddle);
        AddChild(Ball);

        var hudLayer = new CanvasLayer { Name = "HudLayer" };
        var hud = new HudView { Name = "Hud" };
        hudLayer.AddChild(hud);

        var feedback = new FeedbackLayer { Name = "Feedback" };
        hudLayer.AddChild(feedback);

        var overlay = new ResultOverlay { Name = "ResultOverlay" };
        hudLayer.AddChild(overlay);
        AddChild(hudLayer);
    }

    /// <summary>
    ///     生成新关卡砖墙。
    /// </summary>
    private void GenerateLevel()
    {
        ClearBricks();

        var generator = new LevelGenerator();
        var spawns = generator.Generate(SpawnPositions, LevelConfig.Default);

        foreach (var spawn in spawns)
        {
            var data = new Brick(spawn.Type, spawn.Size);
            BrickField.Add(data, spawn.Position);

            var view = new BrickView { Position = ToGodot(spawn.Position) };
            view.Setup(data);
            AddChild(view);
            _brickViews.Add(view);
        }
    }

    /// <summary>
    ///     清空现有砖（视图 + domain）。
    /// </summary>
    private void ClearBricks()
    {
        foreach (var view in _brickViews)
        {
            view.QueueFree();
        }

        _brickViews.Clear();

        foreach (var brick in BrickField.Bricks.ToArray())
        {
            BrickField.HitBrick(brick, 999);
        }
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
    ///     球落底：扣命、广播事件并处理重挂/游戏结束。
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
            return;
        }

        Ball.Dead = false;
        Ball.CanMove = true;
        Ball.AttachToPaddle();
    }

    /// <summary>
    ///     砖被球碰到（未摧毁）：补充能量并广播。
    /// </summary>
    public void OnBrickHit(BrickType visualType)
    {
        Run.OnBrickHit();
        Score.OnBrickTouched();
        this.SendEvent(ChannelConstants.Gameplay,
            new ScoreChangedEvent(Score.Score, Score.Combo));
        this.SendEvent(ChannelConstants.Gameplay,
            new EnergyChangedEvent(Run.Energy, Run.Energy >= RunState.MaxEnergy));
    }

    /// <summary>
    ///     砖被摧毁：计分、统计清场并广播。
    /// </summary>
    public void OnBrickDestroyed(BrickView view, IReadOnlyList<BrickDestroyedResult> results)
    {
        foreach (var result in results)
        {
            Score.OnBrickDestroyed();
            _log.Debug($"砖摧毁 {result.Brick.Type}（{result.Reason}）");
        }

        // 表现：任何摧毁播爆炸声；若含爆炸砖源则强化
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
        }
    }

    /// <summary>
    ///     记录 bump 判定结果并广播（结算统计由订阅方维护或本地汇总）。
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
    ///     生成砖锚点网格（对照原版 SpawnPos 布局：8 列 × 3 行）。
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

    private static Vector2 ToGodot(Vec2 v) => new(v.X, v.Y);
}
