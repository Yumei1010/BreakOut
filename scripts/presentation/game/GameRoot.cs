using System.Collections.Generic;
using System.Linq;
using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.brick;
using BreakOut.scripts.domain.bump;
using BreakOut.scripts.domain.common;
using BreakOut.scripts.domain.level;
using BreakOut.scripts.domain.run;
using BreakOut.scripts.domain.scoring;
using BreakOut.scripts.presentation.ball;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.paddle;

namespace BreakOut.scripts.presentation.game;

/// <summary>
///     玩法场景根节点：组装 domain 实例、生成关卡、驱动各视图并结算事件。
/// </summary>
/// <remarks>
///     单场景游戏不走 UI 页面栈，本节点常驻 main.tscn。
///     持有 domain 单例（RunState/ScoreRule/BrickField），视图薄壳把碰撞/输入转发到这里做规则结算。
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
    private int _earlyBumps;
    private int _lateBumps;
    private int _perfectBumps;

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
    ///     获取板视图。
    /// </summary>
    public PaddleView? Paddle { get; private set; }

    /// <summary>
    ///     获取球视图。
    /// </summary>
    public BallView? Ball { get; private set; }

    /// <summary>
    ///     创建视图节点（场景树构建）。
    /// </summary>
    public override void _Ready()
    {
        BuildScene();
        GenerateLevel();
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
        Paddle = new PaddleView { Position = new Vector2(960, 990) };
        Ball = new BallView { Position = new Vector2(960, 900) };
        AddChild(Paddle);
        AddChild(Ball);
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
            BrickField.HitBrick(brick, 999); // domain 内清场不产生连锁视觉
        }
    }

    /// <summary>
    ///     球落底：扣命并处理重挂/游戏结束。
    /// </summary>
    private void OnBallLost()
    {
        if (Ball == null)
        {
            return;
        }

        Ball.Die();
        var dead = Run.OnBallLost();
        _log.Debug($"球落底，剩余生命 {Run.Health}");

        if (dead)
        {
            _log.Warn("游戏结束");
            return;
        }

        // 还有生命：重置球回板
        Ball.Dead = false;
        Ball.CanMove = true;
        Ball.AttachToPaddle();
    }

    /// <summary>
    ///     砖被球碰到（未摧毁）：连击分数已在视图层累加，这里刷新统计。
    /// </summary>
    /// <param name="visualType">当前砖视觉类型。</param>
    public void OnBrickHit(BrickType visualType)
    {
        Run.OnBrickHit();
    }

    /// <summary>
    ///     砖被摧毁：能量砖回调已在 BrickField 处理，这里统计清场。
    /// </summary>
    public void OnBrickDestroyed(BrickView view, IReadOnlyList<BrickDestroyedResult> results)
    {
        foreach (var result in results)
        {
            Score.OnBrickDestroyed();
            _log.Debug($"砖摧毁 {result.Brick.Type}（{result.Reason}）");
        }

        view.QueueFree();
        _brickViews.Remove(view);

        // 普通砖全清 → 过关
        if (BrickField.AliveCount == 0)
        {
            Run.OnLevelCleared();
            _log.Info("关卡清除！");
        }
    }

    /// <summary>
    ///     记录 bump 判定结果（结算统计）。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    public void OnBumpJudged(BumpGrade grade)
    {
        switch (grade)
        {
            case BumpGrade.Perfect:
                _perfectBumps += 1;
                break;
            case BumpGrade.Late:
                _lateBumps += 1;
                break;
            case BumpGrade.Early:
                _earlyBumps += 1;
                break;
        }
    }

    /// <summary>
    ///     计算当前对局结算结果。
    /// </summary>
    public StageResult BuildStageResult()
    {
        return new StageResult(
            _earlyBumps,
            _lateBumps,
            _perfectBumps,
            Ball?.Bounces ?? 0,
            Score.Score);
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
            // 前两行 8 列，第三行中间 6 列（原版布局）
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
