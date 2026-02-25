using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Devour
{
    public class CompProperties_AbilityDevourPawn : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDevourPawn()
        {
            this.compClass = typeof(CompAbilityEffect_DevourPawn);
        }
    }

    public class CompAbilityEffect_DevourPawn : CompAbilityEffect
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn targetPawn = target.Pawn;
            if (targetPawn == null) return false;

            // 1. 必须是类人生物
            if (!targetPawn.RaceProps.Humanlike)
            {
                if (throwMessages) Messages.Message("这种极致的交尾技巧只能用来吞噬类人生物的躯体。", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 2. 不能是自己
            if (targetPawn == this.parent.pawn)
            {
                if (throwMessages) Messages.Message("无效目标：你的腔道还不能把你自己翻转吞进去。", targetPawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 3. 检查施法者体内是否已经满了 (最大容量 1)
            Hediff holderHediff = this.parent.pawn.health.hediffSet.GetFirstHediffOfDef(RavenDefOf.Raven_Hediff_DevouredPawnHolder);
            if (holderHediff != null)
            {
                var comp = holderHediff.TryGetComp<HediffComp_DevouredPawnHolder>();
                if (comp != null && comp.innerContainer.Count >= 1)
                {
                    if (throwMessages) Messages.Message("体内深处的肉穴已经被猎物彻底填满了，塞不下更多了！", this.parent.pawn, MessageTypeDefOf.RejectInput, false);
                    return false;
                }
            }

            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = this.parent.pawn;
            Pawn victim = target.Pawn;

            if (caster == null || victim == null) return;

            // 1. 生成牵引抛射物
            Projectile_DevourPull proj = (Projectile_DevourPull)GenSpawn.Spawn(RavenDefOf.Raven_Projectile_DevourPull, victim.Position, caster.Map);

            // 2. 将猎物强制从地图上移除，并塞入抛射物的空间内
            victim.DeSpawn(DestroyMode.Vanish);
            proj.innerContainer.TryAdd(victim, false);

            // 3. 将抛射物射向施法者 (使用 DrawPos 匹配重载)
            proj.Launch(caster, victim.DrawPos, caster, caster, ProjectileHitFlags.All);

            // 特效：原地留下空间扭曲
            FleckMaker.ThrowMetaIcon(victim.Position, caster.Map, FleckDefOf.PsycastAreaEffect, 0.5f);
        }
    }
}