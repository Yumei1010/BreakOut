namespace BreakOut.scripts.constants;

/// <summary>
///     预定义频段常量，避免魔法字符串。
///     可根据项目需要扩展（例如：Net 网络频段、Audio 音频频段等）。
/// </summary>
public static class ChannelConstants
{
    /// <summary>
    ///     游戏逻辑频段：核心玩法逻辑（战斗、移动、得分等）。
    /// </summary>
    public const string Gameplay = "Gameplay";

    /// <summary>
    ///     UI 频段：界面相关事件（弹窗、菜单、HUD 更新等）。
    /// </summary>
    public const string Ui = "Ui";

    /// <summary>
    ///     音频频段：声音相关事件（播放音效、切歌等）。
    /// </summary>
    public const string Audio = "Audio";

    /// <summary>
    ///     网络频段：网络同步相关事件（玩家上线、数据同步等）。
    /// </summary>
    public const string Net = "Net";
}
