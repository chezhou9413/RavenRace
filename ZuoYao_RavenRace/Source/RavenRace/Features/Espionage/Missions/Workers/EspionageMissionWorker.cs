using System.Collections.Generic;
using System.Linq; // 确保引用 Linq
using RavenRace.Features.Espionage.Managers;
using RimWorld;
using Verse;

namespace RavenRace.Features.Espionage.Workers
{
    public class EspionageMissionWorker
    {
        public EspionageMissionDef def;

        public virtual bool CanStartNow(Faction target, OfficialData specificTarget, out string reason)
        {
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var data = comp.GetSpyData(target);

            // [逻辑修复] 1. 基础情报 (GatherIntel, specificTarget=null)
            // 如果所有官员都已经 Known，则不能再搜集基础情报 (逼迫玩家去针对特定目标或执行高级任务)
            if (def.missionType == MissionType.GatherIntel && specificTarget == null)
            {
                if (data.allOfficials.All(o => o.isKnown))
                {
                    reason = "已掌握该派系所有官员的基础信息";
                    return false;
                }
            }

            // [逻辑修复] 2. 针对特定目标的任务检查
            if (specificTarget != null)
            {
                // 如果是 ProbeOfficial (MissionType=GatherIntel, 但有目标)，不需要 Known (就是要去 Known 他)
                // 实际上 GatherIntel + specificTarget 就是 Probe
                if (def.missionType == MissionType.GatherIntel)
                {
                    // 允许对未知目标执行刺探
                }
                else
                {
                    // 其他所有针对性任务 (策反、暗杀、贿赂) 必须先 Known
                    if (!specificTarget.isKnown)
                    {
                        reason = "需先【刺探详细资料】解锁目标信息";
                        return false;
                    }
                }

                // 策反逻辑加强：必须先了解其弱点 (这里简化为 Known 且忠诚度不是满的，或者其他条件)
                // 你的描述里说“策反依赖于刺探详细资料”，这就是 isKnown = true
                if (def.missionType == MissionType.Turncoat && !specificTarget.isKnown)
                {
                    reason = "目标身份不明，无法策反";
                    return false;
                }

                // 目标状态检查
                if (def.missionType == MissionType.Turncoat && specificTarget.isTurncoat)
                {
                    reason = "RavenRace_Mission_TargetTurncoated".Translate();
                    return false;
                }
                if (def.missionType == MissionType.Assassinate && specificTarget.isDead)
                {
                    reason = "RavenRace_Mission_TargetDead".Translate();
                    return false;
                }
            }

            // 3. 等级检查
            int requiredLevel = def.difficultyLevel;
            if (specificTarget != null && def.requiresTargetOfficial)
            {
                // Leader(0) -> +3, HighCouncil(1) -> +2, Manager(2) -> +1, Key(3) -> +0
                requiredLevel += (3 - (int)specificTarget.rank);
            }

            if (data.InfiltrationLevel < requiredLevel)
            {
                reason = "RavenRace_Mission_Reason_Level".Translate(requiredLevel);
                return false;
            }

            // 4. 资源检查
            if (FusangResourceManager.GetAmount(FusangResourceType.Intel) < def.costIntel)
            {
                reason = "RavenRace_Mission_Reason_Intel".Translate();
                return false;
            }

            if (def.costMoney > 0 && !TradeUtility.ColonyHasEnoughSilver(Find.CurrentMap, def.costMoney))
            {
                reason = "RavenRace_Mission_CostSilver".Translate();
                return false;
            }

            if (def.costInfluence > 0 && FusangResourceManager.GetAmount(FusangResourceType.Influence) < def.costInfluence)
            {
                reason = "RavenRace_Mission_Reason_Influence".Translate();
                return false;
            }

            reason = null;
            return true;
        }

        // StartMission 保持不变
        public virtual void StartMission(Faction target, SpyData spy, OfficialData specificTarget)
        {
            if (def.costIntel > 0) FusangResourceManager.TryConsume(FusangResourceType.Intel, def.costIntel);
            if (def.costInfluence > 0) FusangResourceManager.TryConsume(FusangResourceType.Influence, def.costInfluence);
            if (def.costMoney > 0) TradeUtility.LaunchSilver(Find.CurrentMap, def.costMoney);

            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var mission = comp.CreateMission(def, spy, target, specificTarget);
            MissionManager.StartMission(mission);
        }

        public virtual void CompleteMission(ActiveMission mission, bool success)
        {
            if (success) MissionOutcomeHandler.ApplySuccess(mission);
            else MissionOutcomeHandler.ApplyFailure(mission);

            if (mission.spy != null)
            {
                mission.spy.currentMission = null;
                mission.spy.state = SpyState.Infiltrating;
            }
        }
    }
}