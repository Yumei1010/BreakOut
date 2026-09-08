namespace BreakOut.scripts.domain.brick;

/// <summary>
///     砖种规格：注册表驱动的砖数据定义。
/// </summary>
/// <param name="Health">初始血量。</param>
/// <param name="SpawnWeight">随机生成权重（决定砖在关卡中的出现比例）。</param>
/// <param name="IsExplosive">摧毁时是否连锁引爆。</param>
/// <param name="IsEnergy">摧毁时是否补充能量。</param>
/// <param name="ImmuneToChain">是否免疫连锁爆炸伤害。</param>
public sealed record BrickSpec(
    int Health,
    float SpawnWeight,
    bool IsExplosive = false,
    bool IsEnergy = false,
    bool ImmuneToChain = false);
