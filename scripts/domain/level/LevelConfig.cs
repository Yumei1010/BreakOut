using System;

namespace BreakOut.scripts.domain.level;

/// <summary>
///     关卡布局配置：砖的生成概率与填充率。
/// </summary>
public sealed class LevelConfig
{
    /// <summary>
    ///     默认配置（对照原版 game.gd 数值）。
    /// </summary>
    public static LevelConfig Default { get; } = new();

    /// <summary>
    ///     创建关卡配置。
    /// </summary>
    /// <param name="fillProbability">每个锚点生成砖的概率（默认 0.9，原版 90% 有砖）。</param>
    /// <param name="longBrickProbability">砖为长砖的概率（默认 0.65）。</param>
    public LevelConfig(float fillProbability = 0.9f, float longBrickProbability = 0.65f)
    {
        FillProbability = fillProbability;
        LongBrickProbability = longBrickProbability;
    }

    /// <summary>
    ///     获取每个锚点生成砖的概率。
    /// </summary>
    public float FillProbability { get; }

    /// <summary>
    ///     获取砖为长砖的概率（原版 65% 长砖）。
    /// </summary>
    public float LongBrickProbability { get; }
}
