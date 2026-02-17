using System;
using Verse;
using RimWorld;
using UnityEngine;
using RavenRace.Features.CustomPawn.ZuoYao.UI; // 引用UI命名空间

namespace RavenRace.Features.CustomPawn.ZuoYao
{
    public class CompProperties_AbilityKotoamatsukami : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityKotoamatsukami()
        {
            this.compClass = typeof(CompAbilityEffect_Kotoamatsukami);
        }
    }

    public class CompAbilityEffect_Kotoamatsukami : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = this.parent.pawn;

            if (targetPawn == null || caster == null) return;

            // 打开自定义配置窗口
            Find.WindowStack.Add(new Dialog_KotoamatsukamiCustomization(caster, targetPawn, this));
        }

        /// <summary>
        /// 最终应用效果 (由 Dialog 调用)
        /// </summary>
        public void ApplyEffectFinal(Pawn caster, Pawn targetPawn)
        {
            bool isHostileOrNeutral = targetPawn.Faction != caster.Faction;

            // 如果是敌对/中立，先进行招募和转化
            if (isHostileOrNeutral)
            {
                ApplyToHostile(targetPawn, caster);
            }
            else
            {
                ApplyToFriendly(targetPawn, caster);
            }

            // 播放特效
            FleckMaker.ThrowMetaIcon(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastAreaEffect, 0.42f);
            Messages.Message("RavenRace_Msg_KotoamatsukamiSuccess".Translate(caster.LabelShort, targetPawn.LabelShort), targetPawn, MessageTypeDefOf.PositiveEvent);
        }

        private void ApplyToFriendly(Pawn target, Pawn caster)
        {
            // 刷新关系：先移除旧的（如果存在），再添加新的
            target.relations.RemoveDirectRelation(ZuoYaoDefOf.Raven_Relation_AbsoluteMaster, caster);
            caster.relations.RemoveDirectRelation(ZuoYaoDefOf.Raven_Relation_LoyalServant, target);

            target.relations.AddDirectRelation(ZuoYaoDefOf.Raven_Relation_AbsoluteMaster, caster);
            caster.relations.AddDirectRelation(ZuoYaoDefOf.Raven_Relation_LoyalServant, target);

            // 具体的数值已保存到 WorldComponent，Harmony 补丁会自动接管
        }

        private void ApplyToHostile(Pawn target, Pawn caster)
        {
            // 1. 清除旧关系和记忆 (洗脑)
            if (target.needs?.mood?.thoughts?.memories != null)
                target.needs.mood.thoughts.memories.Memories.Clear();

            // 暴力清除所有关系可能导致报错，RimWorld通常不建议ClearAllRelations，
            // 但为了效果，我们只针对Faction相关的。这里暂时保留原逻辑，但需谨慎。
            // 更好的做法是只移除敌对派系的关系。为保安全，这里不执行 ClearAllRelations。

            // 2. 强制招募
            RecruitUtility.Recruit(target, caster.Faction, caster);

            // 3. 改写信仰 (如果启用了DLC)
            if (ModsConfig.IdeologyActive && target.Ideo != null && caster.Ideo != null)
            {
                target.ideo.SetIdeo(caster.Ideo);
                target.ideo.OffsetCertainty(1.0f); // 信仰拉满
            }

            // 4. 建立主仆关系
            ApplyToFriendly(target, caster);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null) return false;

            // 视线检查由 XML 属性 requireLineOfSight 处理，这里额外检查致盲
            if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            {
                if (throwMessages) Messages.Message("Invalid Target: Target is blind.", p, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }
}