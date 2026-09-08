using BreakOut.scripts.rules.run;

namespace BreakOut.scripts.rules.ability;

/// <summary>
///     能力触发结果。
/// </summary>
public enum AbilityResult
{
    /// <summary>
    ///     能力成功触发。
    /// </summary>
    Success,

    /// <summary>
    ///     能量不足，无法触发（仅限需要能量的能力）。
    /// </summary>
    InsufficientEnergy,

    /// <summary>
    ///     能力尚在冷却或当前状态不允许。
    /// </summary>
    NotReady
}

/// <summary>
///     板能力规则：判定触发条件与能量消耗（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 paddle.gd：
///     <list type="bullet">
///         <item>Dash：无消耗，带冷却（表现层计时）</item>
///         <item>Laser：需能量恰好满 100，触发后清空</item>
///         <item>Attract：按住持续耗能（每秒 5），有能量即可吸球</item>
///     </list>
/// </remarks>
public sealed class AbilityRule
{
    /// <summary>
    ///     终极激光所需能量（满能量）。
    /// </summary>
    public const float LaserEnergyCost = RunState.MaxEnergy;

    /// <summary>
    ///     吸引每秒能量消耗。
    /// </summary>
    public const float AttractEnergyPerSecond = 5f;

    /// <summary>
    ///     触发冲刺能力。
    /// </summary>
    /// <param name="canDash">是否可冲刺（冷却/状态允许）。</param>
    /// <returns>触发结果。</returns>
    public static AbilityResult TryDash(bool canDash)
    {
        return canDash ? AbilityResult.Success : AbilityResult.NotReady;
    }

    /// <summary>
    ///     尝试触发终极激光：能量必须足够（满 100），触发后由调用方清空能量。
    /// </summary>
    /// <param name="run">对局状态。</param>
    /// <returns>触发结果。</returns>
    public static AbilityResult TryLaser(RunState run)
    {
        if (!run.CanSpend(LaserEnergyCost))
        {
            return AbilityResult.InsufficientEnergy;
        }

        return AbilityResult.Success;
    }

    /// <summary>
    ///     尝试磁力召回：免费能力，仅受冷却限制。
    /// </summary>
    /// <param name="canUse">冷却是否就绪。</param>
    /// <returns>触发结果。</returns>
    public static AbilityResult TryMagnet(bool canUse)
    {
        return canUse ? AbilityResult.Success : AbilityResult.NotReady;
    }

    /// <summary>
    ///     尝试吸引球：按当前帧时长消耗能量，返回消耗是否可行。
    /// </summary>
    /// <param name="run">对局状态。</param>
    /// <param name="deltaSeconds">帧时长（秒）。</param>
    /// <returns>true 表示能量足够并已扣减；false 表示能量不足。</returns>
    public static bool TryAttract(RunState run, float deltaSeconds)
    {
        var cost = AttractEnergyPerSecond * deltaSeconds;
        if (!run.CanSpend(cost))
        {
            return false;
        }

        run.SpendEnergy(cost);
        return true;
    }
}
