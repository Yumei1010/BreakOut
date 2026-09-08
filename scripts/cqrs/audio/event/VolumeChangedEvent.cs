namespace BreakOut.scripts.cqrs.audio.@event;

/// <summary>
///     音量变更事件，当任何音频通道（主音量/BGM/音效）的音量发生变化时触发
/// </summary>
public sealed class VolumeChangedEvent
{
    /// <summary>
    ///     音频通道名称（如 Master、Bgm、Sfx）
    /// </summary>
    public required string Channel { get; init; }

    /// <summary>
    ///     变更后的音量值（0.0 到 1.0）
    /// </summary>
    public required float Volume { get; init; }
}
