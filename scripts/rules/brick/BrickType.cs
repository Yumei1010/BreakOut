namespace BreakOut.scripts.rules.brick;

/// <summary>
///     砖种类型。
/// </summary>
public enum BrickType
{
    /// <summary>
    ///     普通一血砖（概率最高）。
    /// </summary>
    One,

    /// <summary>
    ///     两血砖。
    /// </summary>
    Two,

    /// <summary>
    ///     三血砖。
    /// </summary>
    Three,

    /// <summary>
    ///     爆炸砖：摧毁时对邻近砖造成范围伤害并连锁引爆。
    /// </summary>
    Explosive,

    /// <summary>
    ///     能量砖：摧毁时一次性补充大量能量。
    /// </summary>
    Energy,

    /// <summary>
    ///     金属砖：高血量且免疫连锁爆炸（拓展砖种）。
    /// </summary>
    Metal,

    /// <summary>
    ///     彩虹砖：每次受击变换颜色，摧毁获得高连击分（拓展砖种）。
    /// </summary>
    Rainbow
}
