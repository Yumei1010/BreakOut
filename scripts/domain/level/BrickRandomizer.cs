using System;
using BreakOut.scripts.domain.brick;

namespace BreakOut.scripts.domain.level;

/// <summary>
///     砖种随机选择器：按原版概率表生成砖种与尺寸（纯 C#，可注入随机源单测）。
/// </summary>
/// <remarks>
///     原版 brick.gd 概率表：
///     <list type="bullet">
///         <item>Explosive：5%（rand &lt; 0.05）</item>
///         <item>Energy：10%（0.05 ≤ rand &lt; 0.15）</item>
///         <item>Three：20%（0.15 ≤ rand &lt; 0.35）</item>
///         <item>Two：30%（0.35 ≤ rand &lt; 0.65）</item>
///         <item>One：35%（其余）</item>
///     </list>
///     尺寸：65% 长砖。
/// </remarks>
public static class BrickRandomizer
{
    /// <summary>
    ///     生成砖种类型。
    /// </summary>
    /// <param name="random">随机源（0-1 均匀分布）。</param>
    /// <returns>砖种类型。</returns>
    public static BrickType RollType(Random random)
    {
        var rand = random.NextDouble();
        return rand switch
        {
            < 0.05 => BrickType.Explosive,
            < 0.15 => BrickType.Energy,
            < 0.35 => BrickType.Three,
            < 0.65 => BrickType.Two,
            _ => BrickType.One
        };
    }

    /// <summary>
    ///     生成砖尺寸。
    /// </summary>
    /// <param name="random">随机源（0-1 均匀分布）。</param>
    /// <returns>砖尺寸。</returns>
    public static BrickSize RollSize(Random random)
    {
        return random.NextDouble() < 0.65 ? BrickSize.Long : BrickSize.Small;
    }
}
