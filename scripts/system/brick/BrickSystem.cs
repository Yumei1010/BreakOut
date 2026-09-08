using System.Collections.Generic;
using System.Linq;
using Godot;
using BreakOut.scripts.rules.brick;
using BreakOut.scripts.rules.common;
using BreakOut.scripts.rules.level;
using BreakOut.scripts.entities.brick;

namespace BreakOut.scripts.system.brick;

/// <summary>
///     砖墙系统：持有 BrickField 规则与砖视图注册表，负责砖的生成/受击/清空/存活判定。
/// </summary>
/// <remarks>
///     规则逻辑（连锁/血量/能量）在 <c>rules/brick</c>；本系统做规则到视图的桥接与容器管理。
/// </remarks>
public sealed class BrickSystem
{
    /// <summary>
    ///     砖墙碰撞影响半径（用于爆炸连锁）。
    /// </summary>
    private const float ExplosionRadius = 120f;

    private readonly List<BrickView> _brickViews = new();

    /// <summary>
    ///     获取砖墙规则。
    /// </summary>
    public BrickField Field { get; } = new(ExplosionRadius);

    /// <summary>
    ///     获取场上所有砖视图。
    /// </summary>
    public IReadOnlyList<BrickView> Views => _brickViews;

    /// <summary>
    ///     获取存活砖数。
    /// </summary>
    public int AliveCount => Field.AliveCount;

    /// <summary>
    ///     清空所有砖（视图 + 规则）。
    /// </summary>
    /// <param name="bricksRoot">砖容器节点。</param>
    public void ClearAll(Node bricksRoot)
    {
        foreach (var child in bricksRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _brickViews.Clear();
        ClearRules();
    }

    /// <summary>
    ///     清空规则层砖（无视图时）。
    /// </summary>
    public void ClearRules()
    {
        foreach (var brick in Field.Bricks.ToArray())
        {
            Field.HitBrick(brick, 999);
        }
    }

    /// <summary>
    ///     添加一块砖（规则 + 视图）。
    /// </summary>
    /// <param name="spawn">砖投放数据。</param>
    /// <param name="brickScene">砖场景。</param>
    /// <param name="bricksRoot">砖容器节点。</param>
    public BrickView AddBrick(BrickSpawn spawn, PackedScene brickScene, Node2D bricksRoot)
    {
        var data = new Brick(spawn.Type, spawn.Size);
        Field.Add(data, spawn.Position);

        var view = brickScene.Instantiate<BrickView>();
        view.Position = ToGodot(spawn.Position);
        bricksRoot.AddChild(view);
        view.Setup(data);
        _brickViews.Add(view);
        return view;
    }

    /// <summary>
    ///     砖受击（球/激光），返回连锁摧毁结果。
    /// </summary>
    /// <param name="view">被击砖视图。</param>
    /// <param name="damage">伤害值。</param>
    /// <param name="onEnergyDestroyed">能量砖摧毁回调。</param>
    public IReadOnlyList<BrickDestroyedResult> HitBrick(BrickView view, int damage, System.Action? onEnergyDestroyed = null)
    {
        return Field.HitBrick(view.Data, damage, onEnergyDestroyed);
    }

    /// <summary>
    ///     移除砖视图（摧毁后）。
    /// </summary>
    /// <param name="view">砖视图。</param>
    public void RemoveView(BrickView view)
    {
        _brickViews.Remove(view);
        view.QueueFree();
    }

    private static Vector2 ToGodot(Vec2 v) => new(v.X, v.Y);
}
