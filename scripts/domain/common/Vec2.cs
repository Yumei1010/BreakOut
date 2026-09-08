using System;

namespace BreakOut.scripts.domain.common;

/// <summary>
///     极简二维向量，供 domain 层做纯向量数学，避免依赖 Godot.Vector2 以便单测。
/// </summary>
public readonly record struct Vec2(float X, float Y)
{
    /// <summary>
    ///     零向量。
    /// </summary>
    public static readonly Vec2 Zero = new(0f, 0f);

    /// <summary>
    ///     计算向量长度（模）。
    /// </summary>
    public readonly float Length() => MathF.Sqrt(X * X + Y * Y);

    /// <summary>
    ///     计算单位向量；零向量返回零向量。
    /// </summary>
    public readonly Vec2 Normalized()
    {
        var length = Length();
        return length < 1e-6f ? Zero : new Vec2(X / length, Y / length);
    }

    /// <summary>
    ///     点乘。
    /// </summary>
    public readonly float Dot(Vec2 other) => X * other.X + Y * other.Y;

    /// <summary>
    ///     向量与法线的反弹：v - 2*(v·n)*n。
    /// </summary>
    /// <param name="normal">法线（无需归一化，内部自动处理）。</param>
    public readonly Vec2 Bounce(Vec2 normal)
    {
        var n = normal.Normalized();
        return this - 2f * Dot(n) * n;
    }

    /// <summary>
    ///     线性插值：this 与 target 之间按 t 取点。
    /// </summary>
    public readonly Vec2 Lerp(Vec2 target, float t) =>
        new(X + (target.X - X) * t, Y + (target.Y - Y) * t);

    /// <summary>
    ///     限制向量长度不超过 <paramref name="maxLength"/>。
    /// </summary>
    public readonly Vec2 LimitLength(float maxLength)
    {
        var length = Length();
        return length > maxLength ? new Vec2(X / length * maxLength, Y / length * maxLength) : this;
    }

    /// <summary>
    ///     向量相减。
    /// </summary>
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>
    ///     向量数乘。
    /// </summary>
    public static Vec2 operator *(Vec2 a, float scalar) => new(a.X * scalar, a.Y * scalar);

    /// <summary>
    ///     标量左乘向量。
    /// </summary>
    public static Vec2 operator *(float scalar, Vec2 a) => new(a.X * scalar, a.Y * scalar);
}
