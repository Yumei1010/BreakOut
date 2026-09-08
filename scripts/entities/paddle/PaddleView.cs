using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.entities.game;

namespace BreakOut.scripts.entities.paddle;

/// <summary>
///     板实体（薄壳）：加载 paddle 场景骨架，输入/动效转发，规则判定走能力规则。
/// </summary>
/// <remarks>
///     按 partial 拆分：.cs 核心 / .Dependencies 注入 / .Properties 字段 / .Events 事件 / .Signals 信号。
/// </remarks>
[Log]
[ContextAware]
public partial class PaddleView : CharacterBody2D
{
    /// <summary>
    ///     初始化：引用布局子节点。
    /// </summary>
    public override void _Ready()
    {
        _root = FindGameRoot();
        if (_root == null)
        {
            GD.PushError("PaddleView 未找到 GameRoot");
            return;
        }

        ResolveVisualNodes();
    }

    /// <summary>
    ///     每帧：平滑移动 + 弹簧振荡 + 能力输入。
    /// </summary>
    public override void _Process(double delta)
    {
        if (GetTree().Paused || !_visualReady)
        {
            return;
        }

        var dt = (float)delta;
        var dir = Input.GetActionStrength("right") - Input.GetActionStrength("left");

        if (_dashing)
        {
            return;
        }

        if (dir != 0)
        {
            Velocity = new Vector2(Mathf.Lerp(Velocity.X, dir * Speed, Accel * dt), 0);
        }
        else
        {
            Velocity = new Vector2(Mathf.Lerp(Velocity.X, 0f, Decel * dt), 0);
        }

        // 弹簧振荡器：速度驱动板体摆动
        _oscillatorVelocity += (Velocity.X / Speed) * VelocityMultiplier;
        var force = -Spring * _displacement + Damp * _oscillatorVelocity;
        _oscillatorVelocity -= force * dt;
        _displacement -= _oscillatorVelocity * dt;
        if (_sprite != null)
        {
            _sprite.Rotation = -_displacement;
        }

        HandleAbilityInput();
    }

    /// <summary>
    ///     每物理帧应用移动。
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !_visualReady)
        {
            return;
        }

        if (Velocity.LengthSquared() > 0.001f)
        {
            MoveAndSlide();
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }

    /// <summary>
    ///     球碰板：触发 bounce 动画（bulge 变形 + 压扁回弹）。
    /// </summary>
    public void PlayBounce()
    {
        _anim?.Play("bounce");
    }

    /// <summary>
    ///     玩家按 bump：触发 bump 动画（板上弹）。
    /// </summary>
    public void PlayBumpAnimation()
    {
        _anim?.Stop();
        _anim?.Play("bump");
    }
}
