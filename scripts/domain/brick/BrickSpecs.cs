namespace BreakOut.scripts.domain.brick;

/// <summary>
///     砖种规格查询门面：统一从 <see cref="BrickCatalog"/> 读取砖数据。
/// </summary>
/// <remarks>
///     薄封装保持既有调用点兼容；新增砖种只需注册 <see cref="BrickCatalog"/>。
/// </remarks>
public static class BrickSpecs
{
    /// <summary>
    ///     获取砖种规格。
    /// </summary>
    /// <param name="type">砖种类型。</param>
    /// <returns>砖种规格。</returns>
    public static BrickSpec SpecOf(BrickType type) => BrickCatalog.Specs[type];

    /// <summary>
    ///     获取砖种初始血量。
    /// </summary>
    public static int HealthOf(BrickType type) => BrickCatalog.Specs[type].Health;

    /// <summary>
    ///     获取砖种生成权重。
    /// </summary>
    public static float SpawnWeightOf(BrickType type) => BrickCatalog.Specs[type].SpawnWeight;

    /// <summary>
    ///     判断砖种是否为爆炸砖（摧毁时连锁引爆）。
    /// </summary>
    public static bool IsExplosive(BrickType type) => BrickCatalog.Specs[type].IsExplosive;

    /// <summary>
    ///     判断砖种是否为能量砖（摧毁时补充能量）。
    /// </summary>
    public static bool IsEnergy(BrickType type) => BrickCatalog.Specs[type].IsEnergy;

    /// <summary>
    ///     判断砖种是否免疫连锁爆炸伤害。
    /// </summary>
    public static bool ImmuneToChain(BrickType type) => BrickCatalog.Specs[type].ImmuneToChain;
}
