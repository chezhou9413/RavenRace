using Verse;

namespace RavenRace.Features.Servitude
{
    /// <summary>
    /// 自定义的 Def 类型，用于在 XML 中存储侍奉系统的配置。
    /// XML 加载器会根据标签名 <RavenRace.Features.Servitude.ServitudeConfigDef>
    /// 自动将其实例化并存入 DefDatabase<ServitudeConfigDef>。
    /// </summary>
    public class ServitudeConfigDef : Def
    {
        // 这个类是空的，它只作为一个可被 DefDatabase 识别的容器。
        // 所有的配置数据都存储在它的 modExtensions 中。
    }
}