using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace RavenRace
{
    // Patch PawnGenerator，如果检测到当前剧本有我们的 ScenPart，就强制覆盖 PawnKind
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new System.Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_ScenPart_StartingPawnKind
    {
        [HarmonyPrefix]
        public static void Prefix(ref PawnGenerationRequest request)
        {
            // 1. 只处理开局角色生成
            if (request.Context != PawnGenerationContext.PlayerStarter) return;

            // 2. 检查当前剧本
            if (Find.Scenario == null) return;

            // 3. 查找我们自定义的 ScenPart
            // 注意：在 XML 加载初期可能找不到 Parts，但在生成 Pawn 时肯定有了
            var part = Find.Scenario.AllParts.OfType<ScenPart_StartingPawnKind>().FirstOrDefault();

            if (part != null && part.pawnKind != null)
            {
                // 4. 强制覆盖 PawnKind
                // 因为 request 是 ref 传递的结构体，直接修改生效
                request.KindDef = part.pawnKind;

                // 确保派系是玩家派系 (虽然通常已经是了)
                if (request.Faction == null) request.Faction = Faction.OfPlayer;

                // RavenModUtility.LogVerbose($"[RavenRace] Forced starting pawn kind to {part.pawnKind.defName} via ScenPart.");
            }
        }
    }
}