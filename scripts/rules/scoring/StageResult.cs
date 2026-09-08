namespace BreakOut.scripts.rules.scoring;

/// <summary>
///     单局结算结果，汇总各统计项并计算最终得分。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 stage_clear.gd：
///     最终得分 = 早期击球×500 + 晚期击球×500 + 完美击球×2000 + 反弹次数×10 + 对局得分。
/// </remarks>
public sealed class StageResult
{
    /// <summary>
    ///     早期/晚期击球每次加分。
    /// </summary>
    public const int EarlyLateMultiplier = 500;

    /// <summary>
    ///     完美击球每次加分。
    /// </summary>
    public const int PerfectMultiplier = 2000;

    /// <summary>
    ///     每次反弹加分。
    /// </summary>
    public const int BounceMultiplier = 10;

    /// <summary>
    ///     创建结算结果。
    /// </summary>
    /// <param name="earlyBumps">早期击球次数。</param>
    /// <param name="lateBumps">晚期击球次数。</param>
    /// <param name="perfectBumps">完美击球次数。</param>
    /// <param name="ballBounces">球反弹总次数。</param>
    /// <param name="runScore">对局内累计得分。</param>
    public StageResult(int earlyBumps, int lateBumps, int perfectBumps, int ballBounces, int runScore)
    {
        EarlyBumps = earlyBumps;
        LateBumps = lateBumps;
        PerfectBumps = perfectBumps;
        BallBounces = ballBounces;
        RunScore = runScore;
    }

    /// <summary>
    ///     获取早期击球次数。
    /// </summary>
    public int EarlyBumps { get; }

    /// <summary>
    ///     获取晚期击球次数。
    /// </summary>
    public int LateBumps { get; }

    /// <summary>
    ///     获取完美击球次数。
    /// </summary>
    public int PerfectBumps { get; }

    /// <summary>
    ///     获取球反弹总次数。
    /// </summary>
    public int BallBounces { get; }

    /// <summary>
    ///     获取对局内累计得分。
    /// </summary>
    public int RunScore { get; }

    /// <summary>
    ///     计算最终得分。
    /// </summary>
    public int FinalScore =>
        EarlyBumps * EarlyLateMultiplier
        + LateBumps * EarlyLateMultiplier
        + PerfectBumps * PerfectMultiplier
        + BallBounces * BounceMultiplier
        + RunScore;
}
