using System.Collections.Generic;
using BreakOut.scripts.rules.common;
using BreakOut.scripts.rules.level;

namespace BreakOut.scripts.system.level;

/// <summary>
///     关卡系统：读取锚点并生成砖投放列表（规则在 <c>rules/level</c>）。
/// </summary>
public sealed class LevelSystem
{
    /// <summary>
    ///     根据锚点生成砖投放列表。
    /// </summary>
    /// <param name="anchorPositions">砖锚点位置。</param>
    /// <param name="config">关卡配置。</param>
    public IReadOnlyList<BrickSpawn> Generate(IReadOnlyList<Vec2> anchorPositions, LevelConfig config)
    {
        var generator = new LevelGenerator();
        return generator.Generate(anchorPositions, config);
    }
}
