namespace BreakOut.scripts.domain.brick;

/// <summary>
///     单块砖的对局内状态（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 brick.gd：血量管理、受击伤害、摧毁判定与连锁爆炸由 BrickField 协调。
/// </remarks>
public sealed class Brick
{
    /// <summary>
    ///     创建一块砖。
    /// </summary>
    /// <param name="type">砖种类型。</param>
    /// <param name="size">砖尺寸。</param>
    public Brick(BrickType type, BrickSize size)
    {
        Type = type;
        Size = size;
        Health = BrickSpecs.HealthOf(type);
    }

    /// <summary>
    ///     获取砖种类型。
    /// </summary>
    public BrickType Type { get; private set; }

    /// <summary>
    ///     获取砖尺寸。
    /// </summary>
    public BrickSize Size { get; }

    /// <summary>
    ///     获取当前血量。
    /// </summary>
    public int Health { get; private set; }

    /// <summary>
    ///     获取是否已被摧毁。
    /// </summary>
    public bool IsDestroyed { get; private set; }

    /// <summary>
    ///     获取剩余血量对应的视觉类型（多血砖随血量降级显示）。
    /// </summary>
    public BrickType VisualType => Health switch
    {
        >= 3 => BrickType.Three,
        2 => BrickType.Two,
        _ => BrickType.One
    };

    /// <summary>
    ///     对砖造成伤害；血量归零时标记为已摧毁。
    /// </summary>
    /// <param name="damage">伤害值。</param>
    /// <returns>本次伤害是否导致砖被摧毁。</returns>
    public bool Damage(int damage)
    {
        if (IsDestroyed)
        {
            return false;
        }

        Health -= damage;

        if (Health <= 0)
        {
            IsDestroyed = true;
            return true;
        }

        return false;
    }
}
