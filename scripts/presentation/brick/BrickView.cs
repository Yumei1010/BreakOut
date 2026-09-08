using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.domain.brick;
using BreakOut.scripts.presentation.assets;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.brick;

/// <summary>
///     砖视图（薄壳）：加载 brick_layout 场景骨架，绑定 domain Brick 并转发受击。
/// </summary>
/// <remarks>
///     挂载于 scenes/brick/brick_layout.tscn 根（StaticBody2D）。Size/Type 双层 Sprite 由布局提供，
///     Setup 时按 domain 砖数据切换纹理与碰撞形状；摧毁通知 GameRoot 结算。
/// </remarks>
[Log]
[ContextAware]
public partial class BrickView : StaticBody2D
{
    private GameRoot _root = null!;
    private Sprite2D _sizeSprite = null!;
    private Sprite2D _typeSprite = null!;
    private CollisionShape2D _shapeLong = null!;
    private CollisionShape2D _shapeSmall = null!;
    private bool _hitHandled;
    private bool _visualReady;
    private Tween? _hitTween;

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
    ///     初始化视图。
    /// </summary>
    public override void _Ready()
    {
        _root = FindGameRoot();
        if (_root == null)
        {
            GD.PushError("BrickView 未找到 GameRoot");
            return;
        }

        ResolveVisualNodes();
    }

    private void ResolveVisualNodes()
    {
        _sizeSprite = GetNodeOrNull<Sprite2D>("Size");
        _typeSprite = GetNodeOrNull<Sprite2D>("Type");
        _shapeLong = GetNodeOrNull<CollisionShape2D>("CollisionShapeLong");
        _shapeSmall = GetNodeOrNull<CollisionShape2D>("CollisionShapeSmall");
        _visualReady = _sizeSprite != null && _typeSprite != null;
    }

    /// <summary>
    ///     球击中本砖：转发 domain BrickField 判定并处理视觉。
    /// </summary>
    public void OnBallHit() => OnBallHit(1);

    /// <summary>
    ///     球/激光击中本砖：按伤害值转发 domain 判定并处理视觉。
    /// </summary>
    /// <param name="damage">伤害值。</param>
    public void OnBallHit(int damage)
    {
        if (_hitHandled)
        {
            return;
        }

        var field = _root.BrickField;
        var results = field.HitBrick(Data, damage, onEnergyBrickDestroyed: _root.OnEnergyBrickDestroyed);

        if (Data.IsDestroyed)
        {
            _hitHandled = true;
            _root.OnBrickDestroyed(this, results);
        }
        else
        {
            _root.OnBrickHit(Data.VisualType);
            PlayHitBounce();
        }

        RefreshVisual();
    }

    /// <summary>
    ///     用 domain 数据初始化本视图（由砖墙生成器在实例化后调用）。
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
        if (!_visualReady)
        {
            return;
        }

        // 尺寸：长砖启用 Long 碰撞 / 短砖启用 Small
        var isLong = Data.Size == BrickSize.Long;
        if (_shapeLong != null)
        {
            _shapeLong.Disabled = !isLong;
        }

        if (_shapeSmall != null)
        {
            _shapeSmall.Disabled = isLong;
        }

        var (bgTexture, iconTexture, tint) = SelectTextures();
        _sizeSprite.Texture = bgTexture;
        _typeSprite.Texture = iconTexture;
        _typeSprite.SelfModulate = tint;
    }

    /// <summary>
    ///     选择背景/图标纹理与色调。
    /// </summary>
    private (Texture2D, Texture2D, Color) SelectTextures()
    {
        var isLong = Data.Size == BrickSize.Long;
        var isEffect = BrickSpecs.IsEnergy(Data.Type) || BrickSpecs.IsExplosive(Data.Type)
                       || Data.Type is BrickType.Metal;

        var bg = Data.Type switch
        {
            BrickType.Metal when isLong => GameTextures.BrickLongBorder,
            BrickType.Metal => GameTextures.BrickSmallBorder,
            _ when isEffect => isLong ? GameTextures.BrickLongBorder : GameTextures.BrickSmallBorder,
            _ => isLong ? GameTextures.BrickLongFull : GameTextures.BrickSmallFull
        };

        var (icon, tint) = Data.Type switch
        {
            BrickType.Explosive => (GameTextures.BrickBomb, Colors.White),
            BrickType.Energy => (GameTextures.BrickEnergy, Colors.White),
            BrickType.Metal => (GameTextures.BrickTwo, new Color(0.75f, 0.75f, 0.8f)),
            BrickType.Rainbow => RainbowIcon(),
            _ => Data.VisualType switch
            {
                BrickType.Three => (GameTextures.BrickThree, Colors.White),
                BrickType.Two => (GameTextures.BrickTwo, Colors.White),
                _ => (GameTextures.BrickOne, Colors.White)
            }
        };

        return (bg, icon, tint);
    }

    private (Texture2D, Color) RainbowIcon()
    {
        return Data.VisualType switch
        {
            BrickType.Two => (GameTextures.BrickTwo, new Color(1f, 0.4f, 0.4f)),
            BrickType.Three => (GameTextures.BrickThree, new Color(0.4f, 1f, 0.4f)),
            _ => (GameTextures.BrickOne, new Color(0.4f, 0.6f, 1f))
        };
    }

    /// <summary>
    ///     播放击中弹跳表现（原版 brick.gd bounce：Size 弹性缩放 + 随机旋转回位）。
    /// </summary>
    public void PlayHitBounce()
    {
        if (_sizeSprite == null)
        {
            return;
        }

        if (_hitTween != null && _hitTween.IsRunning())
        {
            _hitTween.Kill();
        }

        _hitTween = CreateTween();
        _hitTween.TweenProperty(_sizeSprite, "scale", new Vector2(1.15f, 1.15f), 0.15)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
        _hitTween.Parallel().TweenProperty(_sizeSprite, "rotation_degrees", (float)GD.RandRange(-10, 10), 0.15)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
        _hitTween.TweenProperty(_sizeSprite, "scale", Vector2.One, 0.2)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
        _hitTween.Parallel().TweenProperty(_sizeSprite, "rotation_degrees", 0f, 0.2)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
    }

    private GameRoot FindGameRoot()
    {
        var node = GetParent();
        while (node != null)
        {
            if (node is GameRoot root)
            {
                return root;
            }

            node = node.GetParent();
        }

        return null;
    }
}
