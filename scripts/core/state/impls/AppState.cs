using GFramework.Core.Abstractions.State;
using GFramework.Core.Extensions;
using GFramework.Core.State;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;

namespace BreakOut.scripts.core.state.impls;

/// <summary>
///     应用默认状态，清除 UI 和场景路由，为推送新页面做准备
/// </summary>
public class AppState : AsyncContextAwareStateBase
{
    /// <summary>
    ///     进入状态：异步清除 UI 与场景，为新页面做准备。
    /// </summary>
    /// <param name="from">来源状态</param>
    public override async Task OnEnterAsync(IState? from)
    {
        var uiRouter = this.GetSystem<IUiRouter>()!;
        await uiRouter.ClearAsync().ConfigureAwait(false);
        await this.GetSystem<ISceneRouter>()!.ClearAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task<bool> CanTransitionToAsync(IState target) => Task.FromResult(true);
}
