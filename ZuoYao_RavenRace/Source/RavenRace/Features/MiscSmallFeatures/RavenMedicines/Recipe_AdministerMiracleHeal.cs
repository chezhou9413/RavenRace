using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.UniqueWeapons.SpiritBeads; // 引用 SpiritBeadsDefOf (绝顶升天)
using RavenRace.Buildings; // 引用 RavenBuildingDefOf (意乱情迷 - 如果定义在那边的话)

namespace RavenRace.Features.MiscSmallFeatures.RavenMedicines
{
    /// <summary>
    /// 手术：使用特效治疗药
    /// 逻辑：治愈所有 Hediff_Injury，添加副作用 Hediff
    /// </summary>
    public class Recipe_AdministerMiracleHeal : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (pawn.Dead) return;

            // 1. 治愈所有伤口 (排除缺失的身体部位/断肢)
            // 我们遍历所有 Hediff，如果是 Injury 就治愈
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs.ToList();
            bool healedAny = false;

            foreach (Hediff h in hediffs)
            {
                // Hediff_Injury 代表物理创伤（包括枪伤、抓伤等）
                // 排除 MissingPart (断肢属于 Hediff_MissingPart)
                if (h is Hediff_Injury injury)
                {
                    // 直接治愈到满
                    injury.Heal(9999f);
                    healedAny = true;
                }
            }

            if (healedAny)
            {
                Messages.Message($"{pawn.LabelShort} 的伤口在药物作用下迅速愈合了。", pawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message($"{pawn.LabelShort} 身上没有可愈合的普通伤口。", pawn, MessageTypeDefOf.NeutralEvent);
            }

            // 2. 添加副作用 Hediff

            // A. 绝顶升天 (High Climax)
            // 检查 Def 是否存在 (引用 SpiritBeadsDefOf 或直接查找)
            HediffDef climaxDef = SpiritBeadsDefOf.Raven_Hediff_HighClimax ?? DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_HighClimax");
            if (climaxDef != null)
            {
                // 持续一整天 (SeverityPerDay 通常为 1，所以设为 1.0)
                // 假设 HighClimax 的消失逻辑是基于 Severity 衰减
                Hediff climax = pawn.health.AddHediff(climaxDef);
                climax.Severity = 1.0f;
            }

            // B. 意乱情迷 (Aphrodisiac Effect)
            // 引用 DefenseDefOf 或直接查找
            HediffDef aphrodisiacDef = DefDatabase<HediffDef>.GetNamedSilentFail("RavenHediff_AphrodisiacEffect");
            if (aphrodisiacDef != null)
            {
                Hediff aphro = pawn.health.AddHediff(aphrodisiacDef);
                aphro.Severity = 1.0f; // 严重程度设为高，触发意乱情迷
            }

            // 3. 消耗物品在 Recipe_Surgery 基类中处理 (ingredients 列表)，但对于 Surgery，通常需要 ItemFilter 正确配置
        }
    }
}