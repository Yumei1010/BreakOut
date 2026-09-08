namespace BreakOut.scripts.domain.bump;

/// <summary>
///     击球时机判定等级，由球碰板后的帧距与距离共同决定。
/// </summary>
public enum BumpGrade
{
    /// <summary>
    ///     距离太远，无增益。
    /// </summary>
    TooFar,

    /// <summary>
    ///     过早击球。
    /// </summary>
    Early,

    /// <summary>
    ///     过晚击球。
    /// </summary>
    Late,

    /// <summary>
    ///     完美时机击球。
    /// </summary>
    Perfect
}
