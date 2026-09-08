namespace BreakOut.scripts.rules.scoring;

/// <summary>
///     对局内计分规则（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 game.gd：
///     <list type="bullet">
///         <item>砖被摧毁：200 × 当前 combo</item>
///         <item>球触碰砖（未摧毁）：50 × 当前 combo</item>
///     </list>
///     combo 在每次触发得分事件前 +1，2 秒内无新事件则归零（计时由表现层驱动 ResetCombo）。
/// </remarks>
public sealed class ScoreRule
{
    /// <summary>
    ///     摧毁一块砖的基础分。
    /// </summary>
    public const int BrickDestroyedBaseScore = 200;

    /// <summary>
    ///     球触碰砖的基础分。
    /// </summary>
    public const int BrickTouchedBaseScore = 50;

    private int _score;

    /// <summary>
    ///     获取当前累计得分。
    /// </summary>
    public int Score => _score;

    /// <summary>
    ///     获取当前连击数。
    /// </summary>
    public int Combo { get; private set; }

    /// <summary>
    ///     重置得分与连击（用于开局/重开）。
    /// </summary>
    public void Reset()
    {
        _score = 0;
        Combo = 0;
    }

    /// <summary>
    ///     连击超时归零（表现层计时器触发）。
    /// </summary>
    public void ResetCombo()
    {
        Combo = 0;
    }

    /// <summary>
    ///     记录一次"砖被摧毁"得分事件：连击 +1 后累加 200 × combo。
    /// </summary>
    public void OnBrickDestroyed()
    {
        Combo += 1;
        _score += BrickDestroyedBaseScore * Combo;
    }

    /// <summary>
    ///     记录一次"球触碰砖"得分事件：连击 +1 后累加 50 × combo。
    /// </summary>
    public void OnBrickTouched()
    {
        Combo += 1;
        _score += BrickTouchedBaseScore * Combo;
    }
}
