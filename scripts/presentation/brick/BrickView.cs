using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.brick;
using BreakOut.scripts.presentation.assets;
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
    private GameRoot _root = null!;
    private Sprite2D _bgSprite = null!;
    private Sprite2D _iconSprite = null!;
    private CollisionShape2D _shape = null!;
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
        if (_bgSprite == null)
        {
            return;
        }

        // 背景：普通砖 Full 底 / 特效砖 Border 底（尺寸由资产决定，仅定缩放）
        var isEffect = BrickSpecs.IsEnergy(Data.Type) || BrickSpecs.IsExplosive(Data.Type)
                       || Data.Type is BrickType.Metal;
        var isLong = Data.Size == BrickSize.Long;
        var bgScale = isLong ? new Vector2(1.0f, 1.0f) : new Vector2(0.55f, 1.0f);
        _bgSprite.Texture = isEffect
            ? (isLong ? GameTextures.BrickLongBorder : GameTextures.BrickSmallBorder)
            : (isLong ? GameTextures.BrickLongFull : GameTextures.BrickSmallFull);
        _bgSprite.Scale = bgScale;

        // 前景：按类型选图标；Metal/Rainbow 复用基础图标 + 色调区分
        var (iconTexture, tint) = SelectIcon();
        _iconSprite.Texture = iconTexture;
        _iconSprite.SelfModulate = tint;
        _iconSprite.Scale = bgScale;

        // 碰撞尺寸：长砖 192×64 / 短砖 96×64
        if (_shape.Shape is RectangleShape2D rect)
        {
            rect.Size = isLong ? new Vector2(192, 64) : new Vector2(96, 64);
        }
    }

    /// <summary>
    ///     选择图标纹理与色调（Metal 金属灰 / Rainbow 随视觉循环变色）。
    /// </summary>
    private (Texture2D, Color) SelectIcon()
    {
        return Data.Type switch
        {
            BrickType.Explosive => (GameTextures.BrickBomb, Colors.White),
            BrickType.Energy => (GameTextures.BrickEnergy, Colors.White),
            BrickType.Metal => (GameTextures.BrickTwo, new Color(0.75f, 0.75f, 0.8f)),
            BrickType.Rainbow => Data.VisualType switch
            {
                BrickType.Two => (GameTextures.BrickTwo, new Color(1f, 0.4f, 0.4f)),
                BrickType.Three => (GameTextures.BrickThree, new Color(0.4f, 1f, 0.4f)),
                _ => (GameTextures.BrickOne, new Color(0.4f, 0.6f, 1f))
            },
            _ => Data.VisualType switch
            {
                BrickType.Three => (GameTextures.BrickThree, Colors.White),
                BrickType.Two => (GameTextures.BrickTwo, Colors.White),
                _ => (GameTextures.BrickOne, Colors.White)
            }
        };
    }

    /// <summary>
    ///     播放击中弹跳表现。
    /// </summary>
    public void PlayHitBounce()
    {
        // 占位：后续 tween 弹性动画
    }

    /// <summary>
    ///     构建视觉节点（双层 Sprite：背景 + 图标）。
    /// </summary>
    private void BuildVisual()
    {
        _bgSprite = new Sprite2D { Name = "Bg" };
        AddChild(_bgSprite);

        _iconSprite = new Sprite2D { Name = "Icon" };
        _iconSprite.ZIndex = 1;
        AddChild(_iconSprite);

        _shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(192, 64) }
        };
        AddChild(_shape);

        AddToGroup("Bricks");

        // 尺寸在 Setup 后随数据刷新
        RefreshVisual();
    }
}
