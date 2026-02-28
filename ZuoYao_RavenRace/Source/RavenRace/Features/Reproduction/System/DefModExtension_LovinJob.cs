using Verse;

namespace RavenRace.Features.Reproduction
{
    /// <summary>
    /// 仅作为标记的扩展。
    /// 任何带有这个扩展的 JobDef 在成功执行完毕后，都会自动为其 Pawn 增加交配次数。
    /// </summary>
    public class DefModExtension_LovinJob : DefModExtension
    {
    }
}