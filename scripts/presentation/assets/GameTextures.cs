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
    public static Texture2D Ball => Load("res://assets/texture/ball/ball.png");

    /// <summary>
    ///     板纹理。
    /// </summary>
    public static Texture2D Paddle => Load("res://assets/texture/paddle/Paddle.png");

    /// <summary>
    ///     背景纹理。
    /// </summary>
    public static Texture2D Background => Load("res://assets/texture/game/background.png");

    /// <summary>
    ///     普通砖背景（长）。
    /// </summary>
    public static Texture2D BrickLongFull => Load("res://assets/texture/brick/BlockLongFull.png");

    /// <summary>
    ///     普通砖背景（短）。
    /// </summary>
    public static Texture2D BrickSmallFull => Load("res://assets/texture/brick/BlockSmallFull.png");

    /// <summary>
    ///     特效砖背景（长，爆炸/能量）。
    /// </summary>
    public static Texture2D BrickLongBorder => Load("res://assets/texture/brick/BlockLongBorder.png");

    /// <summary>
    ///     特效砖背景（短，爆炸/能量）。
    /// </summary>
    public static Texture2D BrickSmallBorder => Load("res://assets/texture/brick/BlockSmallBorder.png");

    /// <summary>
    ///     一血砖图标。
    /// </summary>
    public static Texture2D BrickOne => Load("res://assets/texture/brick/One.png");

    /// <summary>
    ///     两血砖图标。
    /// </summary>
    public static Texture2D BrickTwo => Load("res://assets/texture/brick/Two.png");

    /// <summary>
    ///     三血砖图标。
    /// </summary>
    public static Texture2D BrickThree => Load("res://assets/texture/brick/Three.png");

    /// <summary>
    ///     爆炸砖图标。
    /// </summary>
    public static Texture2D BrickBomb => Load("res://assets/texture/brick/Bomb.png");

    /// <summary>
    ///     能量砖图标。
    /// </summary>
    public static Texture2D BrickEnergy => Load("res://assets/texture/brick/Energy.png");

    private static Texture2D Load(string path)
    {
        return GD.Load<Texture2D>(path);
    }
}
