using System;
using BreakOut.scripts.domain.common;

namespace BreakOut.scripts.domain.ball;

/// <summary>
///     球运动规则（纯向量数学，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 ball.gd 关键公式：
///     <list type="bullet">
///         <item>常规：速度向目标速度方向衰减（lerp），并限制不超过最大速度</item>
///         <item>碰板（板在动）：翻转 y、吸收板速 x×0.6、保持原长、乘以 (增益+基础增益)</item>
///         <item>碰板（板静止）：按离板心偏移量倾斜法线（最大 ±15°），反弹并乘增益</item>
///     </list>
/// </remarks>
public static class BallMotion
{
    /// <summary>
    ///     球基准速度。
    /// </summary>
    public const float Speed = 400f;

    /// <summary>
    ///     速度衰减率（向基准速度收敛）。
    /// </summary>
    public const float Decel = 3f;

    /// <summary>
    ///     最大速度。
    /// </summary>
    public const float MaxSpeed = 1200f;

    /// <summary>
    ///     板边缘最大法线倾斜角（度）。
    /// </summary>
    public const float MaxNormalAngleDeg = 15f;

    /// <summary>
    ///     板半宽（用于计算边缘偏移比例）。
    /// </summary>
    public const float PaddleHalfWidth = 96f;

    /// <summary>
    ///     板速度吸收系数。
    /// </summary>
    public const float PaddleVelocityAbsorb = 0.6f;

    /// <summary>
    ///     反弹基础增益（乘在击球增益之上）。
    /// </summary>
    public const float BounceBaseBoost = 1.15f;

    /// <summary>
    ///     计算每帧速度衰减：向目标速度收敛。
    /// </summary>
    /// <param name="velocity">当前速度。</param>
    /// <param name="targetSpeed">目标速度（基准速度）。</param>
    /// <param name="delta">帧时间。</param>
    /// <returns>衰减后的速度。</returns>
    public static Vec2 DecayToward(Vec2 velocity, float targetSpeed, float delta)
    {
        // 原版：velocity = lerp(velocity, velocity.limit_length(speed), deccel * delta)
        var clamped = velocity.LimitLength(targetSpeed);
        return velocity.Lerp(clamped, Decel * delta);
    }

    /// <summary>
    ///     计算球碰移动中板子的反弹速度。
    /// </summary>
    /// <param name="velocity">碰前速度。</param>
    /// <param name="paddleVelocity">板当前速度。</param>
    /// <param name="boost">击球增益（BumpJudge.BoostOf 结果）。</param>
    /// <returns>反弹后速度（未做最终限速）。</returns>
    public static Vec2 BounceOffMovingPaddle(Vec2 velocity, Vec2 paddleVelocity, float boost)
    {
        var lengthBefore = velocity.Length();
        var v = velocity;
        v = new Vec2(v.X, -v.Y);
        v = new Vec2(v.X + paddleVelocity.X * PaddleVelocityAbsorb, v.Y);
        v = v.Normalized();
        return v * lengthBefore * (boost + BounceBaseBoost);
    }

    /// <summary>
    ///     计算球碰静止板子的反弹速度（边缘角度倾斜）。
    /// </summary>
    /// <param name="velocity">碰前速度。</param>
    /// <param name="collisionOffsetX">碰撞点相对板心的水平偏移。</param>
    /// <param name="boost">击球增益。</param>
    /// <returns>反弹后速度（未做最终限速）。</returns>
    public static Vec2 BounceOffStaticPaddle(Vec2 velocity, float collisionOffsetX, float boost)
    {
        // 原版：amount = distance.x / 96.0; normal 绕竖直方向旋转 ±15°×amount
        var amount = collisionOffsetX / PaddleHalfWidth;
        var angle = MathF.PI / 180f * MaxNormalAngleDeg * amount;
        var tiltedNormal = Rotate(new Vec2(0f, -1f), angle);
        return velocity.Bounce(tiltedNormal) * (boost + BounceBaseBoost);
    }

    /// <summary>
    ///     将向量绕原点旋转指定弧度。
    /// </summary>
    public static Vec2 Rotate(Vec2 v, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vec2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }

    /// <summary>
    ///     限制速度不超过最大速度。
    /// </summary>
    public static Vec2 ClampToMax(Vec2 velocity)
    {
        return velocity.LimitLength(MaxSpeed);
    }
}
