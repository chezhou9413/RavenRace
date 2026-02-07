using System.Collections.Generic;
using System.Linq;
using RavenRace.Features.Espionage.Managers;
using RavenRace.Features.Espionage.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Features.Espionage.Workers
{
    public static class MissionOutcomeHandler
    {
        public static void ApplySuccess(ActiveMission mission)
        {
            var comp = Find.World.GetComponent<WorldComponent_Espionage>();
            var data = comp.GetSpyData(mission.targetFaction);
            var official = mission.targetOfficial;
            var spy = mission.spy;

            string letterText = "";

            // 成功任务增加渗透度
            if (mission.def.rewardIntel > 0)
            {
                data.infiltrationPoints += mission.def.rewardIntel;
                if (data.infiltrationPoints > 100f) data.infiltrationPoints = 100f;
            }

            switch (mission.def.missionType)
            {
                case MissionType.GatherIntel:
                    letterText = $"间谍 {spy.Label} 成功获取了情报，渗透度提升。";
                    if (official != null)
                    {
                        official.isKnown = true;
                        letterText += $"\n\n已掌握 {official.Label} 的详细资料。";
                    }
                    else
                    {
                        UnlockRandomOfficial(data);
                    }
                    break;

                case MissionType.StealSupplies:
                    if (mission.def.rewardItem != null)
                    {
                        Thing thing = ThingMaker.MakeThing(mission.def.rewardItem);
                        thing.stackCount = mission.def.rewardItemCount > 0 ? mission.def.rewardItemCount : 1;
                        Map map = Find.CurrentMap;
                        if (map != null)
                        {
                            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
                            DropPodUtility.DropThingsNear(dropSpot, map, new List<Thing> { thing });
                            letterText = $"间谍 {spy.Label} 成功窃取了物资，已空投至基地。";
                        }
                    }
                    break;

                case MissionType.Bribe:
                    if (official != null)
                    {
                        official.relationToPlayer = Mathf.Min(100, official.relationToPlayer + 20f);
                        official.corruption = Mathf.Min(100, official.corruption + 5f);
                        official.isKnown = true;
                        letterText = $"{official.Label} 接受了贿赂。好感度提升，但变得更加贪婪。";
                    }
                    break;

                case MissionType.Turncoat:
                    if (official != null)
                    {
                        official.isTurncoat = true;
                        official.relationToPlayer = 100f;
                        RevealSubordinates(official);
                        letterText = $"{official.Label} 已被成功策反！他现在是我们安插在 {mission.targetFaction.Name} 高层的内线。";
                    }
                    break;

                case MissionType.Assassinate:
                    if (official != null)
                    {
                        HandleAssassination(mission.targetFaction, official);
                        letterText = $"{official.Label} 已被清除。{mission.targetFaction.Name} 陷入了混乱。";
                    }
                    break;
            }

            Find.LetterStack.ReceiveLetter(
                "RavenRace_Espionage_Mission_Success".Translate(),
                letterText, LetterDefOf.PositiveEvent);
        }

        // [核心修复] 补全 ApplyFailure 方法
        public static void ApplyFailure(ActiveMission mission)
        {
            var spy = mission.spy;
            if (spy == null) return;

            spy.exposure += 30f;

            if (spy.exposure >= 100f)
            {
                spy.state = SpyState.Captured;
                Find.LetterStack.ReceiveLetter(
                    "间谍被捕",
                    $"{spy.Label} 在针对 {mission.targetFaction.Name} 的行动中彻底暴露并被捕获！",
                    LetterDefOf.NegativeEvent);

                var comp = Find.World.GetComponent<WorldComponent_Espionage>();
                var factionData = comp.GetSpyData(mission.targetFaction);
                if (factionData != null)
                {
                    factionData.activeSpies.Remove(spy);
                }
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "RavenRace_Espionage_Mission_Failure".Translate(),
                    "RavenRace_Espionage_Mission_FailureDesc".Translate(spy.Label),
                    LetterDefOf.NegativeEvent);
            }
        }

        private static void HandleAssassination(Faction faction, OfficialData official)
        {
            official.isDead = true;
            if (official.pawnReference != null && !official.pawnReference.Dead)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, 9999f, 999f, -1f, null, official.pawnReference.RaceProps.body.corePart);
                official.pawnReference.Kill(dinfo);
            }
            if (official.rank == OfficialRank.Leader)
            {
                HandleLeaderAssassination(faction, official);
            }
            Faction.OfPlayer.TryAffectGoodwillWith(faction, -50, true, true, HistoryEventDefOf.MemberKilled);
        }

        private static void HandleLeaderAssassination(Faction faction, OfficialData oldLeaderData)
        {
            faction.Notify_LeaderDied();
            if (faction.leader != null)
            {
                OfficialPawnUtility.SnapshotPawnData(oldLeaderData, faction.leader);
                oldLeaderData.pawnReference = faction.leader;
                oldLeaderData.isDead = false;
                oldLeaderData.isKnown = true;
                oldLeaderData.isTurncoat = false;
                oldLeaderData.relationToPlayer = 0;
                oldLeaderData.ClearCache();
                Messages.Message($"情报更新：{faction.Name} 的新领袖是 {oldLeaderData.Label}。", MessageTypeDefOf.NeutralEvent);
            }
        }

        private static void UnlockRandomOfficial(FactionSpyData data)
        {
            var unknown = data.allOfficials.Where(o => !o.isDead && !o.isKnown).ToList();
            if (unknown.Any()) unknown.RandomElement().isKnown = true;
        }

        private static void RevealSubordinates(OfficialData official)
        {
            if (official.subordinates.NullOrEmpty()) return;
            foreach (var sub in official.subordinates)
            {
                sub.isKnown = true;
                RevealSubordinates(sub);
            }
        }
    }
}