using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Devour
{
    /// <summary>
    /// 负责实现“强行拉取”的视觉效果。
    /// 内部存放着被吞没的 Pawn，落点结算时将其转移到体内的肉穴容器中。
    /// </summary>
    public class Projectile_DevourPull : Projectile, IThingHolder
    {
        public ThingOwner innerContainer;

        public Projectile_DevourPull()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn caster = this.Launcher as Pawn;

            // [核心修复] 不管物理上命中的是地板还是墙，只要施法者还活着且在地图上，拉取就视为成功。
            if (caster != null && caster.Spawned && !caster.Dead)
            {
                if (innerContainer.Count > 0)
                {
                    // 确保施法者身上有肉穴容器的 Hediff
                    Hediff holderHediff = caster.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_DevouredPawnHolder);
                    if (holderHediff == null)
                    {
                        holderHediff = caster.health.AddHediff(RavenDefOf.Raven_Hediff_DevouredPawnHolder);
                    }

                    var comp = holderHediff.TryGetComp<HediffComp_DevouredPawnHolder>();
                    if (comp != null)
                    {
                        // 转移到施法者体内
                        innerContainer.TryTransferAllToContainer(comp.innerContainer);

                        // 色情播报
                        string hole = caster.gender == Gender.Female ? "深邃火热的子宫深处" : "紧致温热的直肠深处";
                        Messages.Message($"{caster.LabelShort} 的肉体猛地一阵蠕动，将猎物死死绞入了{hole}……", caster, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
            else
            {
                // 异常兜底：飞到一半施法者死了或被世界删除了，直接把人吐地上
                if (innerContainer.Count > 0)
                {
                    foreach (Thing t in innerContainer)
                    {
                        if (t is Pawn strandedPawn)
                        {
                            PawnComponentsUtility.AddComponentsForSpawn(strandedPawn);
                            strandedPawn.health.AddHediff(HediffDefOf.Anesthetic);
                        }
                    }
                    innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near);
                }
            }

            // 命中特效
            FleckMaker.ThrowMetaIcon(base.Position, base.Map, FleckDefOf.PsycastAreaEffect, 0.4f);
            base.Impact(hitThing, blockedByShield);
        }

        // IThingHolder 接口实现
        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
    }
}