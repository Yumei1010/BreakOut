using BreakOut.scripts.rules.bump;

namespace BreakOut.scripts.system.bump;

/// <summary>
///     击球判定系统：持 BumpJudge 规则，集中 bump 判定与统计。
/// </summary>
public sealed class BumpSystem
{
    /// <summary>
    ///     判定击球时机等级。
    /// </summary>
    /// <param name="framesSinceCollision">距球最近一次碰板帧数。</param>
    /// <param name="distance">球板净距。</param>
    public BumpGrade Judge(int framesSinceCollision, float distance)
    {
        return BumpJudge.Judge(framesSinceCollision, distance);
    }

    /// <summary>
    ///     获取判定增益系数。
    /// </summary>
    public float BoostOf(BumpGrade grade)
    {
        return BumpJudge.BoostOf(grade);
    }
}
