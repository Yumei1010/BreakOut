using System;
using System.Collections.Generic;
using System.Linq;
using BreakOut.scripts.rules.brick;
using BreakOut.scripts.rules.common;

namespace BreakOut.scripts.rules.level;

/// <summary>
///     待生成的砖位（锚点 + 砖规格）。
/// </summary>
/// <param name="Position">锚点位置。</param>
/// <param name="Type">砖种类型。</param>
/// <param name="Size">砖尺寸。</param>
public sealed record BrickSpawn(Vec2 Position, BrickType Type, BrickSize Size);

/// <summary>
///     关卡布局生成器：在锚点上按填充率投放砖（纯 C#，可注入随机源单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 game.gd layout_bricks：对每个生成锚点以 90% 概率投放一块随机砖。
/// </remarks>
public sealed class LevelGenerator
{
    private readonly Random _random;

    /// <summary>
    ///     创建关卡生成器。
    /// </summary>
    /// <param name="random">随机源；缺省使用新实例（内部播种随机）。</param>
    public LevelGenerator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    /// <summary>
    ///     为给定锚点生成砖投放列表。
    /// </summary>
    /// <param name="anchorPositions">砖锚点位置集合。</param>
    /// <param name="config">关卡配置。</param>
    /// <returns>砖投放列表。</returns>
    public IReadOnlyList<BrickSpawn> Generate(IReadOnlyList<Vec2> anchorPositions, LevelConfig config)
    {
        ArgumentNullException.ThrowIfNull(anchorPositions);
        ArgumentNullException.ThrowIfNull(config);

        var spawns = new List<BrickSpawn>(anchorPositions.Count);
        foreach (var position in anchorPositions)
        {
            // 原版：10% 概率跳过该锚点（randf() < 0.1 → continue）
            if (_random.NextDouble() < 1.0 - config.FillProbability)
            {
                continue;
            }

            var type = BrickRandomizer.RollType(_random);
            var size = BrickRandomizer.RollSize(_random);
            spawns.Add(new BrickSpawn(position, type, size));
        }

        return spawns;
    }
}
