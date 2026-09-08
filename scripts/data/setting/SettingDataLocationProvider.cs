using GFramework.Game.Abstractions.Data;

namespace BreakOut.scripts.data.setting;

/// <summary>
///     设置数据位置提供者，根据类型名和命名空间生成数据存储位置
/// </summary>
public class SettingDataLocationProvider : IDataLocationProvider
{
    public IDataLocation GetLocation(Type type)
    {
        return new LocalDataLocation
        {
            Key = type.Name,
            Namespace = "settings"
        };
    }
}
