namespace BreakOut.scripts.rules.brick;

/// <summary>
///     单块砖的对局内状态（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 brick.gd：血量管理、受击伤害、摧毁判定与连锁爆炸由 BrickField 协调。
///     血量/爆炸/免疫等数据取自 <see cref="BrickCatalog"/>（注册表驱动）。
///     Rainbow 砖每次受击在 Two/Three/One 间循环视觉（血量不变），摧毁时连击分翻倍（计分侧处理）。
/// </remarks>
public sealed class Brick
{
    private readonly int _cycleIndex;

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
        _cycleIndex = (int)size; // 以尺寸为种子使相邻彩虹砖颜色错开
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
    ///     获取是否免疫连锁爆炸伤害。
    /// </summary>
    public bool ImmuneToChain => BrickSpecs.ImmuneToChain(Type);

    /// <summary>
    ///     获取剩余血量对应的视觉类型（多血砖随血量降级显示；Rainbow 按受击循环）。
    /// </summary>
    public BrickType VisualType => Type switch
    {
        BrickType.Rainbow => RainbowVisual(),
        _ => Health switch
        {
            >= 3 => BrickType.Three,
            2 => BrickType.Two,
            _ => BrickType.One
        }
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

    /// <summary>
    ///     计算彩虹砖当前视觉（随受击次数在 Two→Three→One 间循环）。
    /// </summary>
    private BrickType RainbowVisual()
    {
        // 受击次数 = 初始血量 - 当前血量
        var hits = HealthOf(Type) - Health;
        var cycle = (hits + _cycleIndex) % 3;
        return cycle switch
        {
            0 => BrickType.Two,
            1 => BrickType.Three,
            _ => BrickType.One
        };
    }

    private static int HealthOf(BrickType type) => BrickSpecs.HealthOf(type);
}
