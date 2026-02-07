using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace
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

            Find.WindowStack.Add(new Dialog_KotoamatsukamiCustomization(caster, targetPawn, this));
        }
        // [修改] 移除参数，因为数值已在 Dialog 中保存到 WorldComponent
        public void ApplyEffectFinal(Pawn caster, Pawn targetPawn)
        {
            bool isHostileOrNeutral = targetPawn.Faction != caster.Faction;

            if (isHostileOrNeutral)
            {
                ApplyToHostile(targetPawn, caster);
            }
            else
            {
                ApplyToFriendly(targetPawn, caster);
            }

            FleckMaker.ThrowMetaIcon(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastAreaEffect, 0.42f);
            Messages.Message($"RavenRace_Msg_KotoamatsukamiSuccess".Translate(caster.LabelShort, targetPawn.LabelShort), targetPawn, MessageTypeDefOf.PositiveEvent);
        }

        private void ApplyToFriendly(Pawn target, Pawn caster)
        {
            // 刷新关系
            target.relations.RemoveDirectRelation(RavenDefOf.Raven_Relation_AbsoluteMaster, caster);
            caster.relations.RemoveDirectRelation(RavenDefOf.Raven_Relation_LoyalServant, target);

            target.relations.AddDirectRelation(RavenDefOf.Raven_Relation_AbsoluteMaster, caster);
            caster.relations.AddDirectRelation(RavenDefOf.Raven_Relation_LoyalServant, target);

            // 刷新 Opinion (为了立即生效，可能需要清除缓存)
            // Harmony 补丁会自动接管数值
        }

        private void ApplyToHostile(Pawn target, Pawn caster)
        {
            // 清除旧关系和记忆
            if (target.needs?.mood?.thoughts?.memories != null)
                target.needs.mood.thoughts.memories.Memories.Clear();
            if (target.relations != null)
                target.relations.ClearAllRelations();

            RecruitUtility.Recruit(target, caster.Faction, caster);

            if (target.Ideo != null && caster.Ideo != null && ModsConfig.IdeologyActive)
            {
                target.ideo.SetIdeo(caster.Ideo);
                target.ideo.OffsetCertainty(1.0f);
            }

            ApplyToFriendly(target, caster);
        }

        private void UpdateOrAddMemory(Pawn subject, Pawn other, float opinionVal)
        {
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamed("Raven_Thought_Kotoamatsukami_Recruited");
            if (def == null || subject.needs?.mood?.thoughts?.memories == null) return;

            var memoryHandler = subject.needs.mood.thoughts.memories;

            // 1. 尝试查找现有的 Memory
            // 我们手动遍历，找到第一个匹配 def 和 otherPawn 的
            Thought_Memory_DynamicSocial existingMem = null;
            foreach (var mem in memoryHandler.Memories)
            {
                if (mem.def == def && mem.otherPawn == other)
                {
                    existingMem = mem as Thought_Memory_DynamicSocial;
                    break;
                }
            }

            if (existingMem != null)
            {
                // 2. 如果找到了，直接修改它
                existingMem.SetOpinion(opinionVal);
                existingMem.Renew(); // 重置时间，防止过期
                // Log.Message($"Updated existing memory for {subject.LabelShort} -> {other.LabelShort}: {opinionVal}");
            }
            else
            {
                // 3. 如果没找到，创建新的
                var newMem = (Thought_Memory_DynamicSocial)ThoughtMaker.MakeThought(def);
                newMem.SetOpinion(opinionVal);
                newMem.otherPawn = other;
                memoryHandler.TryGainMemory(newMem, other);
                // Log.Message($"Added new memory for {subject.LabelShort} -> {other.LabelShort}: {opinionVal}");
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn p = target.Pawn;
            if (p == null) return false;

            if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            {
                if (throwMessages) Messages.Message("Invalid Target: Target is blind.", p, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }
    }
}