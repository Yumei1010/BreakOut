using BreakOut.scripts.domain.scoring;

namespace BreakOut.Tests;

/// <summary>
///     计分规则与结算公式测试。
/// </summary>
public class ScoreRuleTests
{
    [Fact]
    public void OnBrickDestroyed_连击累加与得分递增()
    {
        var rule = new ScoreRule();

        rule.OnBrickDestroyed(); // combo=1 → +200
        rule.OnBrickDestroyed(); // combo=2 → +400
        rule.OnBrickDestroyed(); // combo=3 → +600

        Assert.Equal(3, rule.Combo);
        Assert.Equal(1200, rule.Score);
    }

    [Fact]
    public void OnBrickTouched_使用触碰基数()
    {
        var rule = new ScoreRule();

        rule.OnBrickTouched(); // combo=1 → +50

        Assert.Equal(50, rule.Score);
    }

    [Fact]
    public void OnBrickDestroyed_摧毁与触碰交错_combo共用()
    {
        var rule = new ScoreRule();

        rule.OnBrickTouched();  // combo=1 → +50
        rule.OnBrickDestroyed(); // combo=2 → +400

        Assert.Equal(450, rule.Score);
    }

    [Fact]
    public void ResetCombo_连击归零得分保留()
    {
        var rule = new ScoreRule();
        rule.OnBrickDestroyed();

        rule.ResetCombo();

        Assert.Equal(0, rule.Combo);
        Assert.Equal(200, rule.Score);
    }

    [Fact]
    public void Reset_得分连击全清零()
    {
        var rule = new ScoreRule();
        rule.OnBrickDestroyed();
        rule.OnBrickTouched();

        rule.Reset();

        Assert.Equal(0, rule.Combo);
        Assert.Equal(0, rule.Score);
    }

    [Fact]
    public void StageResult_结算公式对照原版系数()
    {
        var result = new StageResult(
            earlyBumps: 2,
            lateBumps: 1,
            perfectBumps: 1,
            ballBounces: 10,
            runScore: 1200);

        // 2×500 + 1×500 + 1×2000 + 10×10 + 1200 = 4800
        Assert.Equal(4800, result.FinalScore);
    }
}
