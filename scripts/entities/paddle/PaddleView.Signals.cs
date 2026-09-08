using Godot;
using BreakOut.scripts.rules.ability;
using BreakOut.scripts.rules.run;

namespace BreakOut.scripts.entities.paddle;

/// <summary>
///     板实体信号桥：Godot 输入 → 能力触发（bump/dash/special/attract）。
/// </summary>
public partial class PaddleView
{
    /// <summary>
    ///     处理 bump/dash/special/attract 能力键输入。
    /// </summary>
    private void HandleAbilityInput()
    {
        if (Input.IsActionJustPressed("bump"))
        {
            PlayBumpAnimation();
            if (AttachedBall != null)
            {
                AttachedBall.Launch();
                AttachedBall = null;
                _root.Flow.Run.StartBall();
            }
            else if (_root.Ball != null)
            {
                _root.Ball.TryBump(this);
            }
        }

        if (Input.IsActionJustPressed("dash") && !_dashing && _dashCooldownReady)
        {
            if (AbilityRule.TryDash(true) == AbilityResult.Success)
            {
                _dashing = true;
                _dashCooldownReady = false;
                Velocity = new Vector2(Mathf.Sign(Velocity.X == 0 ? 1 : Velocity.X) * DashSpeed, 0);
                StartGhostSpawning();
                GetTree().CreateTimer(DashDuration).Timeout += EndDash;
                GetTree().CreateTimer(1.0).Timeout += () => _dashCooldownReady = true;
            }
        }

        if (Input.IsActionJustPressed("special"))
        {
            if (AbilityRule.TryLaser(_root.Flow.Run) == AbilityResult.Success)
            {
                _root.Flow.Run.SpendEnergy(RunState.MaxEnergy);
                _laser?.Shoot();
                _log.Debug("激光发射");
            }
        }

        if (Input.IsActionJustPressed("attract"))
        {
            if (AbilityRule.TryMagnet(_magnetCooldownReady) != AbilityResult.Success)
            {
                _log.Debug("Magnet 冷却中");
            }
            else if (_root.Ball is { Dead: false } ball)
            {
                _magnetCooldownReady = false;
                GetTree().CreateTimer(2.0).Timeout += () => _magnetCooldownReady = true;
                ball.AttachToPaddle();
                _log.Debug("Magnet 召回球");
            }
        }
    }
}
