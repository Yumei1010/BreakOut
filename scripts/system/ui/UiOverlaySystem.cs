using Godot;
using BreakOut.scripts.entities.ui;
using BreakOut.scripts.rules.bump;

namespace BreakOut.scripts.system.ui;

/// <summary>
///     UI 弹层系统：集中实例化 GameOver/StageClear/Ultimate/BumpTiming 弹层（表现协调）。
/// </summary>
public partial class UiOverlaySystem : Node
{
    private CanvasLayer? _layer;
    private readonly Godot.Collections.Dictionary<string, PackedScene> _cache = new();

    /// <summary>
    ///     绑定弹层挂载的 CanvasLayer。
    /// </summary>
    /// <param name="layer">UI 画布层。</param>
    public void Bind(CanvasLayer layer)
    {
        _layer = layer;
    }

    /// <summary>
    ///     显示游戏结束弹层。
    /// </summary>
    public void ShowGameOver()
    {
        Show<GameOverView>("res://scenes/ui/game_over/game_over.tscn");
    }

    /// <summary>
    ///     显示过关结算弹层。
    /// </summary>
    public void ShowStageClear()
    {
        Show<StageClearView>("res://scenes/ui/stage_clear/stage_clear.tscn");
    }

    /// <summary>
    ///     显示满能量提示（去重）。
    /// </summary>
    public void ShowUltimateReady()
    {
        if (_layer != null && _layer.GetNodeOrNull("UltimateReady") == null)
        {
            var view = Get("res://scenes/ui/ultimate/ultimate_ready.tscn").Instantiate<UltimateReadyView>();
            view.Name = "UltimateReady";
            _layer.AddChild(view);
        }
    }

    /// <summary>
    ///     在位置显示 bump 判定飘字。
    /// </summary>
    /// <param name="grade">判定等级。</param>
    /// <param name="position">飘字位置。</param>
    public void SpawnBumpTiming(BumpGrade grade, Vector2 position)
    {
        if (_layer == null)
        {
            return;
        }

        var view = Get("res://scenes/effects/bump/bump_timing.tscn").Instantiate<BumpTimingView>();
        _layer.AddChild(view);
        view.Position = position;
        view.Setup(grade);
    }

    private void Show<T>(string scenePath) where T : Node
    {
        if (_layer == null)
        {
            return;
        }

        var view = Get(scenePath).Instantiate<T>();
        view.Name = typeof(T).Name;
        _layer.AddChild(view);
        if (view is CanvasItem canvasItem)
        {
            canvasItem.MoveToFront();
        }
    }

    private PackedScene Get(string path)
    {
        if (!_cache.TryGetValue(path, out var packed))
        {
            packed = GD.Load<PackedScene>(path);
            _cache[path] = packed;
        }

        return packed;
    }
}
