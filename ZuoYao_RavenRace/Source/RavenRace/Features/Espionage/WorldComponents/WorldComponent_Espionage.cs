using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using RavenRace.Features.Espionage.Managers;
using RavenRace.Features.Espionage.Utilities; // [修复] 添加缺失的命名空间引用

namespace RavenRace.Features.Espionage
{
    public class WorldComponent_Espionage : WorldComponent
    {
        // 公开数据供 Manager 访问
        public Dictionary<int, FactionSpyData> factionData = new Dictionary<int, FactionSpyData>();
        public List<SpyData> allSpies = new List<SpyData>();
        public List<ActiveMission> activeMissions = new List<ActiveMission>();

        private int nextSpyID = 1;
        private int nextMissionID = 1;
        private int nextOfficialID = 1;

        public WorldComponent_Espionage(World world) : base(world) { }

        // --- 基础 CRUD ---
        public int GetNextSpyID() => nextSpyID++;
        public int GetNextOfficialID() => nextOfficialID++;

        public void AddSpy(SpyData spy) => allSpies.Add(spy);
        public void RemoveSpy(SpyData spy) => allSpies.Remove(spy);
        public List<SpyData> GetAllSpies() => allSpies;

        public FactionSpyData GetSpyData(Faction faction)
        {
            if (faction == null) return null;
            if (!factionData.TryGetValue(faction.loadID, out FactionSpyData data))
            {
                data = new FactionSpyData();
                // [修复] 此处现在可以正确引用 Utilities 命名空间下的类了
                FactionPowerStructureGenerator.GenerateStructureFor(faction, data, ref nextOfficialID);
                factionData[faction.loadID] = data;
            }
            return data;
        }

        public ActiveMission CreateMission(EspionageMissionDef def, SpyData spy, Faction target, OfficialData official)
        {
            return new ActiveMission(nextMissionID++, def, spy, target, official);
        }

        // --- 委托逻辑 ---
        public void DispatchColonist(Pawn pawn, Faction target) => SpyManager.DispatchColonist(pawn, target);
        public void RecallSpy(SpyData spy, Map map) => SpyManager.RecallSpy(spy, map);
        public void StartMission(ActiveMission mission) => MissionManager.StartMission(mission);
        public void DebugFinishAllMissions() => MissionManager.DebugFinishAll();

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            // 委托给 Manager
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                MissionManager.TickMissions(activeMissions);
            }

            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                FactionIntelManager.DailyTick(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref factionData, "factionData", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref allSpies, "allSpies", LookMode.Deep);
            Scribe_Collections.Look(ref activeMissions, "activeMissions", LookMode.Deep);

            Scribe_Values.Look(ref nextSpyID, "nextSpyID", 1);
            Scribe_Values.Look(ref nextMissionID, "nextMissionID", 1);
            Scribe_Values.Look(ref nextOfficialID, "nextOfficialID", 1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (factionData == null) factionData = new Dictionary<int, FactionSpyData>();
                if (allSpies == null) allSpies = new List<SpyData>();
                if (activeMissions == null) activeMissions = new List<ActiveMission>();
            }
        }
    }
}