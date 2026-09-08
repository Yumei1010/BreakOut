using System;
using System.Linq;
using BreakOut.scripts.rules.brick;

namespace BreakOut.scripts.rules.level;

/// <summary>
///     砖种随机选择器：按注册表权重生成砖种与尺寸（纯 C#，可注入随机源单测）。
/// </summary>
/// <remarks>
///     权重来自 <see cref="BrickCatalog.Specs"/> 的 SpawnWeight；基础五砖沿用原版概率比例
///     （Explosive 5/Energy 10/Three 20/Two 30/One 35），Metal/Rainbow 由扩展注册提供。
///     尺寸：65% 长砖。
/// </remarks>
public static class BrickRandomizer
{
    private static readonly (BrickType Type, float Cumulative)[] CumulativeWeights =
        BrickCatalog.Specs
            .Select(pair => (Type: pair.Key, Weight: pair.Value.SpawnWeight))
            .OrderBy(pair => pair.Weight)
            .Aggregate(
                new List<(BrickType, float)>(),
                (list, next) =>
                {
                    var total = list.Count == 0 ? 0f : list[^1].Item2;
                    list.Add((next.Type, total + next.Weight));
                    return list;
                })
            .ToArray();

    private static readonly float TotalWeight = CumulativeWeights[^1].Item2;

    /// <summary>
    ///     生成砖种类型（按注册表权重）。
    /// </summary>
    /// <param name="random">随机源（0-1 均匀分布）。</param>
    /// <returns>砖种类型。</returns>
    public static BrickType RollType(Random random)
    {
        var roll = random.NextDouble() * TotalWeight;
        foreach (var (type, cumulative) in CumulativeWeights)
        {
            if (roll < cumulative)
            {
                return type;
            }
        }

        return BrickType.One;
    }

    /// <summary>
    ///     生成砖尺寸（65% 长砖）。
    /// </summary>
    /// <param name="random">随机源（0-1 均匀分布）。</param>
    /// <returns>砖尺寸。</returns>
    public static BrickSize RollSize(Random random)
    {
        return random.NextDouble() < 0.65 ? BrickSize.Long : BrickSize.Small;
    }
}
