using System;

namespace BreakOut.scripts.domain.run;

/// <summary>
///     对局状态：生命、能量与阶段流转（纯 C#，可单测）。
/// </summary>
/// <remarks>
///     移植自 game_juice_breakout_4 的 game.gd：
///     <list type="bullet">
///         <item>生命 3，球落底 -1，归零进入 GameOver</item>
///         <item>能量 0-100：撞普通砖 +10，能量砖毁 +100，满 100 触发满能量事件（外部播终极就绪）</item>
///         <item>能量消耗（吸引等）由外部调用 SpendEnergy</item>
///         <item>关卡全部砖清除 → StageClear；重开 Reset()</item>
///     </list>
/// </remarks>
public sealed class RunState
{
    /// <summary>
    ///     初始生命数。
    /// </summary>
    public const int MaxHealth = 3;

    /// <summary>
    ///     撞击普通砖获得的能量。
    /// </summary>
    public const float BrickHitEnergy = 10f;

    /// <summary>
    ///     能量砖摧毁获得的能量。
    /// </summary>
    public const float EnergyBrickEnergy = 100f;

    /// <summary>
    ///     能量上限。
    /// </summary>
    public const float MaxEnergy = 100f;

    /// <summary>
    ///     满能量触发回调（外部表现：终极技能就绪提示）。
    /// </summary>
    private readonly Action _onEnergyFull;

    private bool _energyFullNotified;

    /// <summary>
    ///     创建对局状态。
    /// </summary>
    /// <param name="onEnergyFull">能量首次达到上限时的回调。</param>
    public RunState(Action? onEnergyFull = null)
    {
        _onEnergyFull = onEnergyFull ?? (() => { });
        Reset();
    }

    /// <summary>
    ///     获取当前生命。
    /// </summary>
    public int Health { get; private set; }

    /// <summary>
    ///     获取当前能量。
    /// </summary>
    public float Energy { get; private set; }

    /// <summary>
    ///     获取当前阶段。
    /// </summary>
    public RunPhase Phase { get; private set; }

    /// <summary>
    ///     重置对局（重开/下一关共用）。
    /// </summary>
    public void Reset()
    {
        Health = MaxHealth;
        Energy = 0f;
        Phase = RunPhase.Ready;
        _energyFullNotified = false;
    }

    /// <summary>
    ///     发球，进入进行中阶段。
    /// </summary>
    public void StartBall()
    {
        Phase = RunPhase.Playing;
    }

    /// <summary>
    ///     球落底：生命 -1；归零进入 GameOver。
    /// </summary>
    /// <returns>true 表示玩家死亡（GameOver）；false 表示还有生命。</returns>
    public bool OnBallLost()
    {
        Health -= 1;
        if (Health <= 0)
        {
            Phase = RunPhase.GameOver;
            return true;
        }

        // 还有生命：回到待发球
        Phase = RunPhase.Ready;
        return false;
    }

    /// <summary>
    ///     撞击普通砖：补充能量，达上限触发回调。
    /// </summary>
    public void OnBrickHit()
    {
        AddEnergy(BrickHitEnergy);
    }

    /// <summary>
    ///     能量砖被摧毁：一次性补满能量。
    /// </summary>
    public void OnEnergyBrickDestroyed()
    {
        AddEnergy(EnergyBrickEnergy);
    }

    /// <summary>
    ///     增加能量并钳制在 [0, 100]，首次达到上限触发回调。
    /// </summary>
    /// <param name="amount">增加量。</param>
    public void AddEnergy(float amount)
    {
        var before = Energy;
        Energy = Math.Clamp(Energy + amount, 0f, MaxEnergy);

        if (!_energyFullNotified && Energy >= MaxEnergy && before < MaxEnergy)
        {
            _energyFullNotified = true;
            _onEnergyFull();
        }
    }

    /// <summary>
    ///     消耗能量（钳制不低于 0）。
    /// </summary>
    /// <param name="amount">消耗量。</param>
    public void SpendEnergy(float amount)
    {
        Energy = Math.Clamp(Energy - amount, 0f, MaxEnergy);
        if (Energy < MaxEnergy)
        {
            _energyFullNotified = false; // 能量回落，允许下次再次触发满能量
        }
    }

    /// <summary>
    ///     判断是否有足够能量执行一次满能量消耗动作。
    /// </summary>
    /// <param name="amount">需要消耗的能量。</param>
    public bool CanSpend(float amount) => Energy >= amount;

    /// <summary>
    ///     关卡全部砖清除。
    /// </summary>
    public void OnLevelCleared()
    {
        Phase = RunPhase.StageClear;
    }
}
