namespace BreakOut.scripts.domain.bump;

/// <summary>
///     Bump 判定核心逻辑（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 ball.gd bump_boost 逻辑：
///     <list type="bullet">
///         <item>距离 &gt; 最大击球距离 → TooFar（增益系数 1.0）</item>
///         <item>帧距 &lt; 5 → Perfect（增益 1.3）</item>
///         <item>帧距 &gt; 5 且 &lt; 20 → Late（增益 1.15）</item>
///         <item>其余（含帧距恰好为 5）→ Early（增益 1.15）</item>
///     </list>
///     注意：原版 <c>frames_since_paddle_collision == 5</c> 不满足任一分支（&gt;5 与 &lt;5 均不命中），落入 Early。
///     此处为保持手感一致保留该边界行为。
/// </remarks>
public static class BumpJudge
{
    /// <summary>
    ///     最大击球距离（像素），超过则判定 TooFar。
    /// </summary>
    public const float MaxBumpDistance = 40.0f;

    /// <summary>
    ///     完美击球帧窗口（帧距小于该值判 Perfect）。
    /// </summary>
    public const int PerfectFrameWindow = 5;

    /// <summary>
    ///     Late 帧窗口下限（帧距需大于该值）。
    /// </summary>
    public const int LateFrameLowerBound = 5;

    /// <summary>
    ///     Late 帧窗口上限（帧距需小于该值）。
    /// </summary>
    public const int LateFrameUpperBound = 20;

    /// <summary>
    ///     Perfect 增益系数。
    /// </summary>
    public const float PerfectBoost = 1.3f;

    /// <summary>
    ///     Late/Early 增益系数。
    /// </summary>
    public const float LateEarlyBoost = 1.15f;

    /// <summary>
    ///     无增益时的系数（TooFar / 未主动 bump）。
    /// </summary>
    public const float NoBoost = 1.0f;

    /// <summary>
    ///     判定击球时机等级。
    /// </summary>
    /// <param name="framesSinceCollision">距球最近一次碰板的帧数。</param>
    /// <param name="distance">球与板的接触距离（球心到板面的垂直净距）。</param>
    /// <returns>判定等级。</returns>
    public static BumpGrade Judge(int framesSinceCollision, float distance)
    {
        if (distance > MaxBumpDistance)
        {
            return BumpGrade.TooFar;
        }

        if (framesSinceCollision < PerfectFrameWindow)
        {
            return BumpGrade.Perfect;
        }

        if (framesSinceCollision > LateFrameLowerBound && framesSinceCollision < LateFrameUpperBound)
        {
            return BumpGrade.Late;
        }

        return BumpGrade.Early;
    }

    /// <summary>
    ///     获取判定等级对应的速度增益系数。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    /// <returns>增益系数。</returns>
    public static float BoostOf(BumpGrade grade)
    {
        return grade switch
        {
            BumpGrade.Perfect => PerfectBoost,
            BumpGrade.Late or BumpGrade.Early => LateEarlyBoost,
            _ => NoBoost
        };
    }
}
