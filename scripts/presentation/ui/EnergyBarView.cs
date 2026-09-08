using Godot;
using GFramework.Core.Extensions;
using GFramework.Core.SourceGenerators.Abstractions.Logging;
using GFramework.Core.SourceGenerators.Abstractions.Rule;
using GFramework.Godot.Extensions;
using BreakOut.scripts.constants;
using BreakOut.scripts.cqrs.run.@event;
using BreakOut.scripts.rules.run;
using BreakOut.scripts.utility.@event;

namespace BreakOut.scripts.presentation.ui;

/// <summary>
///     能量条视图：挂 energy_bar.tscn 根，订阅 EnergyChangedEvent 刷新原版 UI。
/// </summary>
/// <remarks>
///     对照原版 energy_bar.gd：进度条 value + 粒子数量按能量比例 + 高能量(90+)抖动。
/// </remarks>
[Log]
[ContextAware]
public partial class EnergyBarView : Control
{
    private const int MaxParticles = 300;

    private ProgressBar _progress = null!;
    private GpuParticles2D _gpuParticles = null!;

    /// <summary>
    ///     初始化：引用子节点并订阅事件。
    /// </summary>
    public override void _Ready()
    {
        _progress = GetNodeOrNull<ProgressBar>("EnergyBar") ?? new ProgressBar();
        _gpuParticles = GetNodeOrNull<GpuParticles2D>("EnergyBar/GPUParticles2D");

        this.RegisterEvent<EnergyChangedEvent>(ChannelConstants.Gameplay, OnEnergyChanged)
            .UnRegisterWhenNodeExitTree(this);

        OnEnergyChanged(new EnergyChangedEvent(0f, false));
    }

    private void OnEnergyChanged(EnergyChangedEvent e)
    {
        _progress.Value = e.Energy;
        if (_gpuParticles != null)
        {
            _gpuParticles.Amount = (int)Mathf.Clamp(e.Energy / RunState.MaxEnergy * MaxParticles, 1, MaxParticles);
        }
    }
}
