using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage.Managers
{
    public static class MissionManager
    {
        public static void StartMission(ActiveMission mission)
        {
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            if (comp == null) return;
            comp.activeMissions.Add(mission);
            if (mission.spy != null)
            {
                mission.spy.currentMission = mission;
                mission.spy.state = SpyState.OnMission;
            }
            Messages.Message("RavenRace_Mission_Started".Translate(mission.def.label, mission.spy?.Label ?? "未知"), MessageTypeDefOf.TaskCompletion);
        }

        public static void TickMissions(List<ActiveMission> missions)
        {
            if (missions.NullOrEmpty()) return;

            for (int i = missions.Count - 1; i >= 0; i--)
            {
                var mission = missions[i];
                if (Find.TickManager.TicksGame >= mission.startTick + mission.durationTicks)
                {
                    CompleteMission(mission, missions);
                }
            }
        }

        private static void CompleteMission(ActiveMission mission, List<ActiveMission> list)
        {
            float successChance = mission.def.baseSuccessChance;
            if (mission.spy != null) successChance += mission.spy.statOperation / 200f;
            if (mission.targetOfficial != null)
            {
                if (mission.def.missionType == MissionType.Turncoat)
                {
                    successChance -= (mission.targetOfficial.loyalty / 200f);
                }
                successChance -= (3 - (int)mission.targetOfficial.rank) * 0.1f;
            }

            bool success = Rand.Chance(UnityEngine.Mathf.Clamp01(successChance));

            // [修复 CS1501] 调用 worker 时传入正确的参数
            mission.def.Worker.CompleteMission(mission, success);

            list.Remove(mission);
        }

        public static void DebugFinishAll()
        {
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            if (comp == null) return;

            for (int i = comp.activeMissions.Count - 1; i >= 0; i--)
            {
                var mission = comp.activeMissions[i];
                mission.def.Worker.CompleteMission(mission, true);
            }
            comp.activeMissions.Clear();
            Messages.Message("所有活跃任务已强制完成。", MessageTypeDefOf.PositiveEvent);
        }
    }
}