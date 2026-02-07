using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
// [新增] 引用堕落刻印功能的DefOf，以便访问新特性
using RavenRace.Features.DegradationCharm;

namespace RavenRace.Features.BedSharing
{
    /// <summary>
    /// 提供与渡鸦族床上共享相关的静态工具方法。
    /// </summary>
    public static class RavenBedSharingUtility
    {
        /// <summary>
        /// 判断一个Pawn是否应被视为“渡鸦”以用于床上共享逻辑。
        /// </summary>
        /// <param name="pawn">要检查的Pawn。</param>
        /// <returns>如果是渡鸦族或拥有特定转化特性，则为true。</returns>
        public static bool IsRaven(Pawn pawn)
        {
            if (pawn == null) return false;

            // [核心修改] 
            // 原始判断：仅检查种族是否为 "Raven_Race"。
            // 新增判断：如果 pawn 拥有“淫堕狂宴”特性，也将其视为“渡鸦”，以实现暖床功能的兼容。
            bool isRavenByRace = pawn.def.defName == "Raven_Race";
            bool isRavenByTrait = pawn.story?.traits?.HasTrait(DegradationCharmDefOf.Raven_Trait_Lecherous) ?? false;

            return isRavenByRace || isRavenByTrait;
        }

        /// <summary>
        /// 判断两个Pawn之间是否应该触发依偎（snuggle）效果。
        /// </summary>
        /// <param name="sleeper">睡眠者。</param>
        /// <param name="partner">床伴。</param>
        /// <returns>如果至少有一方是“渡鸦”，则为true。</returns>
        public static bool ShouldTriggerRavenSnuggle(Pawn sleeper, Pawn partner)
        {
            if (sleeper == null || partner == null) return false;

            bool sleeperIsRaven = IsRaven(sleeper);
            bool partnerIsRaven = IsRaven(partner);

            // 只要双方中至少有一个是“渡鸦”，就触发效果。
            return sleeperIsRaven || partnerIsRaven;
        }

        /// <summary>
        /// 执行依偎效果，包括给予心情和社交增益。
        /// </summary>
        /// <param name="sleeper">睡眠者。</param>
        /// <param name="partner">床伴。</param>
        public static void DoSnuggleEffect(Pawn sleeper, Pawn partner)
        {
            // --- 1. 个人心情 (Mood) ---
            // 为非渡鸦方添加“毛茸茸的暖床”心情。
            if (IsRaven(partner) && !IsRaven(sleeper))
            {
                sleeper.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Raven_Thought_Snuggle"));
            }
            else if (IsRaven(sleeper) && !IsRaven(partner))
            {
                partner.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Raven_Thought_Snuggle"));
            }
            else if (IsRaven(sleeper) && IsRaven(partner)) // 如果双方都是“渡鸦”，则双方都获得心情
            {
                sleeper.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Raven_Thought_Snuggle"));
                partner.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("Raven_Thought_Snuggle"));
            }

            // --- 2. 社交互动 (Opinion) ---
            if (sleeper.RaceProps.Humanlike && partner.RaceProps.Humanlike)
            {
                InteractionDef intDef = DefDatabase<InteractionDef>.GetNamed("Raven_Interaction_Snuggle");
                ThoughtDef socialThoughtDef = DefDatabase<ThoughtDef>.GetNamed("Raven_Thought_Snuggle_Social");

                // 检查是否已经有这个社交记忆了，防止刷屏和重复加好感。
                bool alreadyHasThought = false;
                if (sleeper.needs?.mood?.thoughts?.memories != null)
                {
                    var memories = sleeper.needs.mood.thoughts.memories.Memories;
                    foreach (var mem in memories)
                    {
                        if (mem.def == socialThoughtDef && mem.otherPawn == partner)
                        {
                            alreadyHasThought = true;
                            mem.Renew(); // 刷新持续时间
                            break;
                        }
                    }
                }

                // 只有当没有社交Buff时，才执行完整的交互流程
                if (!alreadyHasThought)
                {
                    // 添加社交心情，增加双方好感度
                    if (intDef.initiatorThought != null && sleeper.needs?.mood != null)
                        Pawn_InteractionsTracker.AddInteractionThought(sleeper, partner, intDef.initiatorThought);

                    if (intDef.recipientThought != null && partner.needs?.mood != null)
                        Pawn_InteractionsTracker.AddInteractionThought(partner, sleeper, intDef.recipientThought);

                    // 显示交互气泡
                    MoteMaker.MakeInteractionBubble(
                        sleeper,
                        partner,
                        intDef.interactionMote,
                        intDef.GetSymbol(sleeper.Faction, sleeper.Ideo),
                        intDef.GetSymbolColor(sleeper.Faction)
                    );

                    // 记录到游戏日志
                    var logEntry = new PlayLogEntry_Interaction(intDef, sleeper, partner, new List<RulePackDef>());
                    Find.PlayLog.Add(logEntry);
                }
            }
        }
    }
}