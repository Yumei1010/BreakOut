using System.Collections.Generic;

namespace BreakOut.scripts.domain.brick;

/// <summary>
///     砖种注册表：集中定义所有砖种数据。
/// </summary>
/// <remarks>
///     新增砖种 = 加枚举值 + 在此注册一条规格，其余逻辑（生成/受击/连锁）无需改动。
///     权重说明：基础五砖沿用原版概率（Explosive 5/Energy 10/Three 20/Two 30/One 35），
///     Metal/Rainbow 为扩展砖种，仅占少量权重。
/// </remarks>
public static class BrickCatalog
{
    /// <summary>
    ///     砖种注册表（枚举 → 规格）。
    /// </summary>
    public static readonly IReadOnlyDictionary<BrickType, BrickSpec> Specs =
        new Dictionary<BrickType, BrickSpec>
        {
            [BrickType.One] = new(Health: 1, SpawnWeight: 35f),
            [BrickType.Two] = new(Health: 2, SpawnWeight: 30f),
            [BrickType.Three] = new(Health: 3, SpawnWeight: 20f),
            [BrickType.Explosive] = new(Health: 1, SpawnWeight: 5f, IsExplosive: true),
            [BrickType.Energy] = new(Health: 1, SpawnWeight: 10f, IsEnergy: true),

            // ---- 拓展示范：新增砖种仅需注册一条 ----
            [BrickType.Metal] = new(Health: 4, SpawnWeight: 6f, ImmuneToChain: true),
            [BrickType.Rainbow] = new(Health: 1, SpawnWeight: 4f),
        };
}
