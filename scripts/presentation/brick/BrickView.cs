using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.brick;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.brick;

/// <summary>
///     砖视图（薄壳）：Godot 静态物理体，绑定 domain Brick 并转发受击。
/// </summary>
/// <remarks>
///     视觉状态（纹理/大小）由 domain Brick 的类型与血量决定；摧毁时通知 GameRoot 结算。
/// </remarks>
[Log]
[ContextAware]
public partial class BrickView : StaticBody2D
{
    private static readonly Vector2 SmallSize = new(96, 32);
    private static readonly Vector2 LongSize = new(192, 32);

    private GameRoot _root = null!;
    private ColorRect _body = null!;
    private bool _hitHandled;

    /// <summary>
    ///     获取绑定的 domain 砖。
    /// </summary>
    public Brick Data { get; private set; } = null!;

    /// <summary>
    ///     获取砖是否属于能量/爆炸类型（球不反弹而穿过）。
    /// </summary>
    public bool IsEnergyOrExplosive =>
        BrickSpecs.IsEnergy(Data.Type) || BrickSpecs.IsExplosive(Data.Type);

    /// <summary>
    ///     初始化视图（由工厂在实例化后调用）。
    /// </summary>
    public override void _Ready()
    {
        _root = GetParent<GameRoot>();
        if (_root == null)
        {
            _root = GetTree().CurrentScene.GetNodeOrNull<GameRoot>(".");
        }

        BuildVisual();
    }

    /// <summary>
    ///     球击中本砖：转发 domain BrickField 判定并处理视觉。
    /// </summary>
    public void OnBallHit()
    {
        if (_hitHandled)
        {
            return;
        }

        var field = _root.BrickField;
        var results = field.HitBrick(Data, 1, onEnergyBrickDestroyed: _root.Run.OnEnergyBrickDestroyed);

        if (Data.IsDestroyed)
        {
            _hitHandled = true;
            _root.OnBrickDestroyed(this, results);
        }
        else
        {
            _root.OnBrickHit(Data.VisualType);
        }

        RefreshVisual();
    }

    /// <summary>
    ///     用 domain 数据初始化本视图（由砖墙生成器调用）。
    /// </summary>
    /// <param name="data">domain 砖。</param>
    public void Setup(Brick data)
    {
        Data = data;
        RefreshVisual();
    }

    /// <summary>
    ///     刷新视觉以匹配 domain 状态。
    /// </summary>
    public void RefreshVisual()
    {
        if (_body == null)
        {
            return;
        }

        var size = Data.Size == BrickSize.Long ? LongSize : SmallSize;
        _body.Size = size;
        _body.Color = ColorFor(Data.VisualType, Data.Type);
        UpdateShape(size);
    }

    /// <summary>
    ///     播放击中弹跳表现。
    /// </summary>
    public void PlayHitBounce()
    {
        // 占位：后续 tween 弹性动画
    }

    private void UpdateShape(Vector2 size)
    {
        foreach (var child in GetChildren())
        {
            if (child is CollisionShape2D shape && shape.Shape is RectangleShape2D rect)
            {
                rect.Size = size;
            }
        }
    }

    /// <summary>
    ///     构建视觉节点。
    /// </summary>
    private void BuildVisual()
    {
        _body = new ColorRect { Name = "Body" };
        AddChild(_body);

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = SmallSize }
        };
        AddChild(shape);

        AddToGroup("Bricks");
    }

    private static Color ColorFor(BrickType visual, BrickType raw)
    {
        return raw switch
        {
            BrickType.Explosive => new Color(1.0f, 0.3f, 0.2f),
            BrickType.Energy => new Color(0.3f, 1.0f, 0.5f),
            _ => visual switch
            {
                BrickType.Three => new Color(0.95f, 0.6f, 0.1f),
                BrickType.Two => new Color(0.3f, 0.7f, 1.0f),
                _ => new Color(0.8f, 0.8f, 0.9f)
            }
        };
    }
}
