using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.entities.game;
using BreakOut.scripts.rules.brick;

namespace BreakOut.scripts.entities.brick;

/// <summary>
///     砖实体（薄壳）：加载 brick 场景骨架，绑定规则 Brick 并转发受击。
/// </summary>
/// <remarks>
///     挂载于 scenes/brick/brick.tscn 根（StaticBody2D）。Size/Type 双层 Sprite 由布局提供，
///     Setup 时按规则砖数据切换纹理与碰撞形状；摧毁通知 GameRoot 结算。
///     按 partial 拆分：.cs 核心 / .Dependencies 节点注入 / .Properties 字段 / .Events 事件 / .Signals 信号。
/// </remarks>
[Log]
[ContextAware]
public partial class BrickView : StaticBody2D
{
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

        // 预置砖（无规则数据）：禁用碰撞形状，避免球撞到未初始化砖
        if (Data == null)
        {
            _shapeLong?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            _shapeSmall?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            SetPhysicsProcess(false);
        }
    }

    /// <summary>
    ///     球击中本砖：转发规则 BrickField 判定并处理视觉。
    /// </summary>
    public void OnBallHit() => OnBallHit(1);

    /// <summary>
    ///     球/激光击中本砖：按伤害值转发规则判定并处理视觉。
    /// </summary>
    /// <param name="damage">伤害值。</param>
    public void OnBallHit(int damage)
    {
        if (_hitHandled || Data == null)
        {
            return;
        }

        var field = _root.Bricks.Field;
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
    ///     用规则数据初始化本实体（由砖墙系统在实例化后调用）。
    /// </summary>
    /// <param name="data">规则砖。</param>
    public void Setup(Brick data)
    {
        Data = data;
        RefreshVisual();
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
}
