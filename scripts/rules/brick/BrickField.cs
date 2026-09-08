using System;
using System.Collections.Generic;
using System.Linq;
using BreakOut.scripts.rules.common;

namespace BreakOut.scripts.rules.brick;

/// <summary>
///     砖块摧毁事件结果。
/// </summary>
/// <param name="Brick">被摧毁的砖。</param>
/// <param name="Reason">摧毁原因。</param>
public sealed record BrickDestroyedResult(Brick Brick, BrickDestroyReason Reason);

/// <summary>
///     砖块摧毁原因。
/// </summary>
public enum BrickDestroyReason
{
    /// <summary>
    ///     球直接击中摧毁。
    /// </summary>
    BallHit,

    /// <summary>
    ///     被爆炸砖连锁摧毁。
    /// </summary>
    ChainExplosion
}

/// <summary>
///     砖墙（一组砖 + 位置），管理受击、摧毁与爆炸连锁（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 game.gd/brick.gd：
///     <list type="bullet">
///         <item>砖被摧毁时若为爆炸砖，对爆炸半径内的其他砖造成 10 点伤害并连锁</item>
///         <item>连锁过程中新引爆的爆炸砖继续扩散（有向环保护）</item>
///     </list>
/// </remarks>
public sealed class BrickField
{
    /// <summary>
    ///     爆炸砖的连锁伤害值。
    /// </summary>
    public const int ExplosionDamage = 10;

    private readonly Dictionary<Brick, Vec2> _bricks = new();
    private readonly float _explosionRadius;

    /// <summary>
    ///     创建砖墙。
    /// </summary>
    /// <param name="explosionRadius">爆炸影响半径（像素）。</param>
    public BrickField(float explosionRadius)
    {
        _explosionRadius = explosionRadius;
    }

    /// <summary>
    ///     获取砖墙内所有砖。
    /// </summary>
    public IReadOnlyCollection<Brick> Bricks => _bricks.Keys;

    /// <summary>
    ///     获取砖墙中尚未摧毁的砖数量。
    /// </summary>
    public int AliveCount => _bricks.Keys.Count(b => !b.IsDestroyed);

    /// <summary>
    ///     向砖墙添加一块砖。
    /// </summary>
    /// <param name="brick">砖实例。</param>
    /// <param name="position">砖的全局位置。</param>
    public void Add(Brick brick, Vec2 position)
    {
        _bricks[brick] = position;
    }

    /// <summary>
    ///     球击中一块砖：扣血，摧毁时处理能量砖回调与爆炸连锁。
    /// </summary>
    /// <param name="brick">被击中的砖。</param>
    /// <param name="damage">伤害值。</param>
    /// <param name="onEnergyBrickDestroyed">能量砖被摧毁时的回调（外部补充能量）。</param>
    /// <returns>本次击中产生的摧毁事件列表（含连锁）。</returns>
    public IReadOnlyList<BrickDestroyedResult> HitBrick(
        Brick brick,
        int damage,
        Action? onEnergyBrickDestroyed = null)
    {
        var results = new List<BrickDestroyedResult>();

        if (!_bricks.TryGetValue(brick, out _))
        {
            return results;
        }

        if (!brick.Damage(damage))
        {
            return results;
        }

        ResolveDestroy(brick, BrickDestroyReason.BallHit, onEnergyBrickDestroyed, results);
        return results;
    }

    /// <summary>
    ///     解析一块已摧毁砖的后果：能量砖触发回调；爆炸砖连锁引爆范围内其他砖。
    /// </summary>
    private void ResolveDestroy(
        Brick brick,
        BrickDestroyReason reason,
        Action? onEnergyBrickDestroyed,
        List<BrickDestroyedResult> results)
    {
        results.Add(new BrickDestroyedResult(brick, reason));

        if (BrickSpecs.IsEnergy(brick.Type))
        {
            onEnergyBrickDestroyed?.Invoke();
        }

        if (BrickSpecs.IsExplosive(brick.Type))
        {
            ChainExplode(brick, onEnergyBrickDestroyed, results);
        }
    }

    /// <summary>
    ///     从爆炸砖位置向半径内所有存活砖造成连锁伤害。
    /// </summary>
    private void ChainExplode(Brick source, Action? onEnergyBrickDestroyed, List<BrickDestroyedResult> results)
    {
        var origin = _bricks[source];

        // 快照避免迭代中修改集合
        foreach (var pair in _bricks.Where(p => !p.Key.IsDestroyed).ToArray())
        {
            if (ReferenceEquals(pair.Key, source))
            {
                continue;
            }

            var distance = (pair.Value - origin).Length();
            if (distance > _explosionRadius)
            {
                continue;
            }

            var target = pair.Key;
            if (target.ImmuneToChain)
            {
                // 金属砖免疫连锁，但仍可能被后续直接命中摧毁
                continue;
            }

            if (target.Damage(ExplosionDamage))
            {
                ResolveDestroy(target, BrickDestroyReason.ChainExplosion, onEnergyBrickDestroyed, results);
            }
        }
    }
}
