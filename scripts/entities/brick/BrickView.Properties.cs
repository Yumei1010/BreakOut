using Godot;
using BreakOut.scripts.rules.brick;
using BreakOut.scripts.utility.effects;

namespace BreakOut.scripts.entities.brick;

/// <summary>
///     砖实体属性：字段、视觉数据与纹理选择。
/// </summary>
public partial class BrickView
{
    /// <summary>
    ///     获取绑定的规则砖。
    /// </summary>
    public Brick Data { get; private set; } = null!;

    /// <summary>
    ///     获取砖是否属于能量/爆炸类型（球不反弹而穿过）。
    /// </summary>
    public bool IsEnergyOrExplosive =>
        BrickSpecs.IsEnergy(Data.Type) || BrickSpecs.IsExplosive(Data.Type);

    private Sprite2D _sizeSprite = null!;
    private Sprite2D _typeSprite = null!;
    private CollisionShape2D _shapeLong = null!;
    private CollisionShape2D _shapeSmall = null!;
    private bool _hitHandled;
    private bool _visualReady;
    private Tween? _hitTween;

    /// <summary>
    ///     刷新视觉以匹配规则状态。
    /// </summary>
    public void RefreshVisual()
    {
        if (!_visualReady)
        {
            return;
        }

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
}
