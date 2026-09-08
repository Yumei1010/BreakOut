using BreakOut.scripts.domain.bump;

namespace BreakOut.Tests;

/// <summary>
///     BumpJudge 判定逻辑测试：帧距与距离边界。
/// </summary>
public class BumpJudgeTests
{
    [Fact]
    public void Judge_距离超过上限_判定TooFar()
    {
        var grade = BumpJudge.Judge(0, BumpJudge.MaxBumpDistance + 1f);

        Assert.Equal(BumpGrade.TooFar, grade);
        Assert.Equal(1.0f, BumpJudge.BoostOf(grade));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void Judge_帧距小于5_判定Perfect(int frames)
    {
        var grade = BumpJudge.Judge(frames, 0f);

        Assert.Equal(BumpGrade.Perfect, grade);
        Assert.Equal(1.3f, BumpJudge.BoostOf(grade));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(19)]
    public void Judge_帧距6到19_判定Late(int frames)
    {
        var grade = BumpJudge.Judge(frames, 0f);

        Assert.Equal(BumpGrade.Late, grade);
        Assert.Equal(1.15f, BumpJudge.BoostOf(grade));
    }

    [Theory]
    [InlineData(5)]   // 恰好 5：原版不满足 >5 与 <5 任一分支 → Early
    [InlineData(20)]
    [InlineData(100)]
    public void Judge_帧距其余范围_判定Early(int frames)
    {
        var grade = BumpJudge.Judge(frames, 0f);

        Assert.Equal(BumpGrade.Early, grade);
        Assert.Equal(1.15f, BumpJudge.BoostOf(grade));
    }
}
