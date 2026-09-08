using Godot;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using BreakOut.scripts.presentation.brick;
using BreakOut.scripts.presentation.game;

namespace BreakOut.scripts.presentation.paddle;

/// <summary>
///     激光视图：挂 laser.tscn，Paddle 满能量时 shoot——显形并伤害 Area2D 内砖（原版 laser.gd）。
/// </summary>
[Log]
[ContextAware]
public partial class LaserView : Area2D
{
    private const float AttackDuration = 0.4f;

    private bool _attacking;
    private GameRoot _root = null!;
    private Timer _attackTimer = null!;

    /// <summary>
    ///     初始化：默认隐藏，绑定攻击计时器。
    /// </summary>
    public override void _Ready()
    {
        Visible = false;
        _root = FindGameRoot();
        _attackTimer = GetNodeOrNull<Timer>("AttackTime") ?? new Timer { WaitTime = AttackDuration, OneShot = true };
        if (GetNodeOrNull<Timer>("AttackTime") == null)
        {
            AddChild(_attackTimer);
        }

        _attackTimer.Timeout += Hide;
        BodyEntered += OnBodyEntered;
    }

    /// <summary>
    ///     发射激光：显形并伤害范围内砖（Paddle special 满能量调用）。
    /// </summary>
    public void Shoot()
    {
        Visible = true;
        _attacking = true;
        _attackTimer.Start();

        // 伤害已在内区域的砖
        foreach (var body in GetOverlappingBodies())
        {
            DamageBrick(body);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_attacking)
        {
            return;
        }

        DamageBrick(body);
    }

    private void DamageBrick(Node2D body)
    {
        if (body is BrickView brick && !brick.Data.IsDestroyed)
        {
            brick.OnBallHit(100);
        }
    }

    private void Hide()
    {
        Visible = false;
        _attacking = false;
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
