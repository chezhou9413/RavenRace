using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Compat.Cinder
{
    public class CompProperties_RavenRegen : HediffCompProperties
    {
        public int checkInterval = 600;
        public float healPerDay = 10f; // 默认给个基础值
        public float healPerDayUnlocked = 50f;
        public int maxPartHpForLocked = 10;
        public string unlockedString = "Unlocked";

        public CompProperties_RavenRegen()
        {
            this.compClass = typeof(CompRavenRegen);
        }
    }

    /// <summary>
    /// 复刻自 Embergarden.HediffComp_Regen
    /// 实现不依赖基因的肉体再生逻辑
    /// </summary>
    public class CompRavenRegen : HediffComp
    {
        private CompProperties_RavenRegen Props => (CompProperties_RavenRegen)this.props;

        // 存档数据
        public bool Unlocked = false;

        // 缓存列表，避免每帧分配内存
        private static List<Hediff> hediffsToHeal = new List<Hediff>();
        private static List<BodyPartRecord> ignoreParts = new List<BodyPartRecord>();

        private float HealPerDay => Unlocked ? Props.healPerDayUnlocked : Props.healPerDay;
        private float HealPerCycle => HealPerDay * (float)Props.checkInterval / 60000f;

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref Unlocked, "Unlocked", false);
        }

        public override string CompLabelPrefix => Unlocked ? Props.unlockedString : null;

        public override string CompLabelInBracketsExtra => $"{HealPerDay} HP/d";

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!Pawn.IsHashIntervalTick(Props.checkInterval)) return;

            hediffsToHeal.Clear();
            ignoreParts.Clear();
            bool eyeInhibited = false;

            // 1. 扫描所有 Hediff
            foreach (Hediff hediff in Pawn.health.hediffSet.hediffs)
            {
                // 记录义肢部位，避免再生
                if (hediff is Hediff_AddedPart)
                {
                    foreach (BodyPartRecord item in hediff.Part.GetPartAndAllChildParts())
                    {
                        ignoreParts.Add(item);
                    }
                }

                // 检查是否有烟烬族的眼睛再生抑制 (通过 DefName 查找，软兼容)
                if (hediff.def.defName == "Cinder_EyeRegenInhibited")
                {
                    eyeInhibited = true;
                }
                else
                {
                    if (hediff is Hediff_Injury)
                    {
                        hediffsToHeal.Add(hediff);
                    }
                    else if (hediff is Hediff_MissingPart missingPart)
                    {
                        // 检查缺失部位是否已有义肢或未被完全移除
                        if (Pawn.health.hediffSet.GetMissingPartFor(missingPart.Part.parent) == null &&
                            !Pawn.health.hediffSet.GetInjuredParts().Contains(missingPart.Part.parent))
                        {
                            hediffsToHeal.Add(hediff);
                        }
                    }
                }
            }

            // 打乱顺序，随机治疗
            hediffsToHeal.Shuffle();

            if (hediffsToHeal.Count > 0)
            {
                float remainingHealAmount = HealPerCycle;
                int count = hediffsToHeal.Count;

                // 2. 执行治疗循环
                // 注意：原版逻辑是在循环中修改集合，这里使用副本或倒序比较安全，但为了复刻原逻辑结构，我们小心处理
                for (int i = 0; i < hediffsToHeal.Count; i++)
                {
                    if (remainingHealAmount <= 0f) break;

                    Hediff targetHediff = hediffsToHeal[i];

                    // A. 治疗伤口
                    if (targetHediff is Hediff_Injury)
                    {
                        float amountToHeal = Math.Min(remainingHealAmount / count, targetHediff.Severity);
                        targetHediff.Severity -= amountToHeal;
                        remainingHealAmount -= amountToHeal;
                        count--; // 剩余目标减少，分摊剩余治疗量
                    }
                    // B. 再生缺失部位
                    else
                    {
                        // 眼睛抑制检查
                        bool isSightSource = targetHediff.Part.def.tags.Contains(BodyPartTagDefOf.SightSource);
                        if (eyeInhibited && isSightSource) continue;

                        if (targetHediff is Hediff_MissingPart missingPart)
                        {
                            if (ignoreParts.Contains(missingPart.Part)) continue;

                            // 检查是否允许再生 (基于血量锁定逻辑)
                            if (!CannotRegenPart(missingPart.Part))
                            {
                                float maxHealth = missingPart.Part.def.GetMaxHealth(Pawn);
                                float healShare = remainingHealAmount / count;

                                // 如果一次分配的治疗量不足以完全长出部位
                                if (healShare < maxHealth)
                                {
                                    // 移除"缺失"状态，添加一个"切伤"状态并设置严重度
                                    // 这样下次循环就会把它当做普通伤口治疗，表现为慢慢长出来
                                    BodyPartRecord part = missingPart.Part;
                                    Hediff injury = HediffMaker.MakeHediff(missingPart.lastInjury ?? HediffDefOf.Cut, Pawn, part);

                                    Pawn.health.RemoveHediff(targetHediff);
                                    Pawn.health.AddHediff(injury, part);
                                    injury.Severity = maxHealth - healShare;

                                    remainingHealAmount -= healShare;
                                }
                                else
                                {
                                    // 直接完全修复
                                    remainingHealAmount -= maxHealth;
                                    Pawn.health.RemoveHediff(targetHediff);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool CannotRegenPart(BodyPartRecord part)
        {
            if (Unlocked) return false;
            // 如果未解锁高级再生，且部位最大血量超过限制，则无法再生
            return part.def.hitPoints > Props.maxPartHpForLocked;
        }
    }
}