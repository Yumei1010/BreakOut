using System;
using BreakOut.scripts.rules.ball;
using BreakOut.scripts.rules.common;
using BreakOut.scripts.rules.bump;

namespace BreakOut.Tests;

/// <summary>
///     球运动数学测试：衰减/移动板反弹/静止板倾斜反弹/限速。
/// </summary>
public class BallMotionTests
{
    [Fact]
    public void DecayToward_超速球收敛到基准速度()
    {
        // 2000 向下飞，limit(400) 后仍是向下 400，lerp 因子 0.05 → 1920
        var fast = new Vec2(0f, 2000f);

        var result = BallMotion.DecayToward(fast, BallMotion.Speed, 1f / 60f);

        Assert.True(result.Length() < 2000f);
        Assert.True(result.Y > 0, "方向不变仍向下");
    }

    [Fact]
    public void DecayToward_慢于基准速度_维持原速()
    {
        // 原版公式只把速度向 limit_length(speed) 收敛：未超速时 limit 无效 → 不变
        var slow = new Vec2(0f, 100f);

        var result = BallMotion.DecayToward(slow, BallMotion.Speed, 1f / 60f);

        Assert.Equal(100f, result.Length(), 1);
    }

    [Fact]
    public void BounceOffMovingPaddle_翻转y并吸收板x速度()
    {
        // 球向下 400（y 正=向下），板向右 500 → 反弹应向上且带右向分量
        var ball = new Vec2(0f, 400f);
        var paddle = new Vec2(500f, 0f);

        var result = BallMotion.BounceOffMovingPaddle(ball, paddle, BumpJudge.NoBoost);

        Assert.True(result.Y < 0, "反弹后应向上（y 负）");
        Assert.True(result.X > 0, "吸收板速度后应有向右分量");
        // 原长保持 × (1 + 1.15)
        Assert.InRange(result.Length(), 400f * 2.14f, 400f * 2.16f);
    }

    [Fact]
    public void BounceOffMovingPaddle_Perfect增益更大()
    {
        var ball = new Vec2(0f, 400f);
        var paddle = new Vec2(500f, 0f);

        var normal = BallMotion.BounceOffMovingPaddle(ball, paddle, BumpJudge.NoBoost);
        var perfect = BallMotion.BounceOffMovingPaddle(ball, paddle, BumpJudge.PerfectBoost);

        Assert.True(perfect.Length() > normal.Length());
    }

    [Fact]
    public void BounceOffStaticPaddle_板心碰撞_垂直反弹()
    {
        // 球向下飞碰板心：偏移 0 → 法线不倾斜，y 翻转向上
        var ball = new Vec2(0f, 400f);

        var result = BallMotion.BounceOffStaticPaddle(ball, collisionOffsetX: 0f, boost: BumpJudge.NoBoost);

        Assert.True(MathF.Abs(result.X) < 0.001f, "板心碰撞无水平分量");
        Assert.True(result.Y < 0, "反弹后向上");
    }

    [Fact]
    public void BounceOffStaticPaddle_边缘碰撞_水平分量增大()
    {
        var ball = new Vec2(0f, 400f);

        var center = BallMotion.BounceOffStaticPaddle(ball, collisionOffsetX: 0f, boost: BumpJudge.NoBoost);
        var edge = BallMotion.BounceOffStaticPaddle(ball, collisionOffsetX: BallMotion.PaddleHalfWidth, boost: BumpJudge.NoBoost);

        Assert.True(MathF.Abs(edge.X) > MathF.Abs(center.X), "边缘碰撞应产生更大水平分量");
    }

    [Fact]
    public void ClampToMax_超速被限制()
    {
        var result = BallMotion.ClampToMax(new Vec2(0f, 5000f));

        Assert.Equal(BallMotion.MaxSpeed, result.Length(), 1);
    }
}
