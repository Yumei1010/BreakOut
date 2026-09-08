using BreakOut.scripts.rules.bump;
using BreakOut.scripts.rules.run;
using BreakOut.scripts.rules.scoring;

namespace BreakOut.scripts.system.scoring;

/// <summary>
///     计分系统：持有 ScoreRule 与局内统计，协调计分/连击/能量/结算。
/// </summary>
/// <remarks>
///     纯 C# 调度（不依赖 Godot），由 GameRoot/事件桥调用。规则逻辑在 <c>rules/scoring</c>。
/// </remarks>
public sealed class ScoringSystem
{
    private int _earlyBumps;
    private int _lateBumps;
    private int _perfectBumps;
    private double _elapsedTime;

    /// <summary>
    ///     获取计分规则。
    /// </summary>
    public ScoreRule Score { get; } = new();

    /// <summary>
    ///     获取对局状态（生命/能量）。
    /// </summary>
    public RunState Run { get; } = new();

    /// <summary>
    ///     触碰砖（未毁）：触碰分 + 连击。
    /// </summary>
    public void OnBrickTouched()
    {
        Score.OnBrickTouched();
    }

    /// <summary>
    ///     砖被摧毁：摧毁分 + 连击。
    /// </summary>
    public void OnBrickDestroyed()
    {
        Score.OnBrickDestroyed();
    }

    /// <summary>
    ///     撞击普通砖：补能量。
    /// </summary>
    public void OnBrickHitEnergy()
    {
        Run.OnBrickHit();
    }

    /// <summary>
    ///     能量砖被摧毁：补满能量。
    /// </summary>
    public void OnEnergyBrickDestroyed()
    {
        Run.OnEnergyBrickDestroyed();
    }

    /// <summary>
    ///     消耗能量（激光/吸引）。
    /// </summary>
    /// <param name="amount">消耗量。</param>
    public void SpendEnergy(float amount)
    {
        Run.SpendEnergy(amount);
    }

    /// <summary>
    ///     记录 bump 判定统计。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    public void RecordBump(BumpGrade grade)
    {
        switch (grade)
        {
            case BumpGrade.Perfect:
                _perfectBumps++;
                break;
            case BumpGrade.Late:
                _lateBumps++;
                break;
            case BumpGrade.Early:
                _earlyBumps++;
                break;
        }
    }

    /// <summary>
    ///     累计对局时间（Playing 阶段）。
    /// </summary>
    /// <param name="delta">帧时间。</param>
    public void TickTime(double delta)
    {
        if (Run.Phase == RunPhase.Playing)
        {
            _elapsedTime += delta;
        }
    }

    /// <summary>
    ///     构建对局结算结果。
    /// </summary>
    /// <param name="ballBounces">球反弹次数（由视图统计提供）。</param>
    public StageResult BuildStageResult(int ballBounces)
    {
        return new StageResult(_earlyBumps, _lateBumps, _perfectBumps, ballBounces, Score.Score);
    }

    /// <summary>
    ///     重置本局统计与规则。
    /// </summary>
    public void Reset()
    {
        _earlyBumps = 0;
        _lateBumps = 0;
        _perfectBumps = 0;
        _elapsedTime = 0;
        Score.Reset();
        Run.Reset();
    }
}
