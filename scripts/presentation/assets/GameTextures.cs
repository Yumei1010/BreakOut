using Godot;

namespace BreakOut.scripts.presentation.assets;

/// <summary>
///     游戏纹理加载器：集中管理资产路径，避免视图代码散落魔法字符串。
/// </summary>
public static class GameTextures
{
    /// <summary>
    ///     球纹理。
    /// </summary>
    public static Texture2D Ball => Load("res://scenes/ball/visuals/ball.png");

    /// <summary>
    ///     板纹理。
    /// </summary>
    public static Texture2D Paddle => Load("res://scenes/paddle/visuals/Paddle.png");

    /// <summary>
    ///     背景纹理。
    /// </summary>
    public static Texture2D Background => Load("res://scenes/game/visuals/background.png");

    /// <summary>
    ///     普通砖背景（长）。
    /// </summary>
    public static Texture2D BrickLongFull => Load("res://scenes/brick/visuals/BlockLongFull.png");

    /// <summary>
    ///     普通砖背景（短）。
    /// </summary>
    public static Texture2D BrickSmallFull => Load("res://scenes/brick/visuals/BlockSmallFull.png");

    /// <summary>
    ///     特效砖背景（长，爆炸/能量）。
    /// </summary>
    public static Texture2D BrickLongBorder => Load("res://scenes/brick/visuals/BlockLongBorder.png");

    /// <summary>
    ///     特效砖背景（短，爆炸/能量）。
    /// </summary>
    public static Texture2D BrickSmallBorder => Load("res://scenes/brick/visuals/BlockSmallBorder.png");

    /// <summary>
    ///     一血砖图标。
    /// </summary>
    public static Texture2D BrickOne => Load("res://scenes/brick/visuals/One.png");

    /// <summary>
    ///     两血砖图标。
    /// </summary>
    public static Texture2D BrickTwo => Load("res://scenes/brick/visuals/Two.png");

    /// <summary>
    ///     三血砖图标。
    /// </summary>
    public static Texture2D BrickThree => Load("res://scenes/brick/visuals/Three.png");

    /// <summary>
    ///     爆炸砖图标。
    /// </summary>
    public static Texture2D BrickBomb => Load("res://scenes/brick/visuals/Bomb.png");

    /// <summary>
    ///     能量砖图标。
    /// </summary>
    public static Texture2D BrickEnergy => Load("res://scenes/brick/visuals/Energy.png");

    private static Texture2D Load(string path)
    {
        return GD.Load<Texture2D>(path);
    }
}
