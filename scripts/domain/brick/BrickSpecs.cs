namespace BreakOut.scripts.domain.brick;

/// <summary>
///     砖种静态配置（血量与基础属性）。
/// </summary>
public static class BrickSpecs
{
    /// <summary>
    ///     获取砖种初始血量。
    /// </summary>
    /// <param name="type">砖种类型。</param>
    /// <returns>初始血量。</returns>
    public static int HealthOf(BrickType type)
    {
        return type switch
        {
            BrickType.Two => 2,
            BrickType.Three => 3,
            _ => 1
        };
    }

    /// <summary>
    ///     判断砖种是否为爆炸砖（摧毁时连锁引爆）。
    /// </summary>
    public static bool IsExplosive(BrickType type) => type == BrickType.Explosive;

    /// <summary>
    ///     判断砖种是否为能量砖（摧毁时补充能量）。
    /// </summary>
    public static bool IsEnergy(BrickType type) => type == BrickType.Energy;
}
