using Godot;
using BreakOut.scripts.entities.game;

namespace BreakOut.scripts.entities.brick;

/// <summary>
///     砖实体依赖注入：场景节点引用解析与 GameRoot 查找。
/// </summary>
public partial class BrickView
{
    private GameRoot _root = null!;

    /// <summary>
    ///     解析布局场景提供的视觉/碰撞子节点。
    /// </summary>
    private void ResolveVisualNodes()
    {
        _sizeSprite = GetNodeOrNull<Sprite2D>("Size");
        _typeSprite = GetNodeOrNull<Sprite2D>("Type");
        _shapeLong = GetNodeOrNull<CollisionShape2D>("CollisionShapeLong");
        _shapeSmall = GetNodeOrNull<CollisionShape2D>("CollisionShapeSmall");
        _visualReady = _sizeSprite != null && _typeSprite != null;
    }

    /// <summary>
    ///     向上查找 GameRoot。
    /// </summary>
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
