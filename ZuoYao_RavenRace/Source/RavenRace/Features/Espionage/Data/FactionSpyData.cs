using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage
{
    public class FactionSpyData : IExposable
    {
        public float infiltrationPoints = 0f;
        public float dailyIntelGain = 0f;

        public FactionControlStatus controlStatus = FactionControlStatus.Independent;

        public OfficialData leaderOfficial;
        public List<OfficialData> allOfficials = new List<OfficialData>();
        public List<SpyData> activeMissionsSpies = new List<SpyData>(); // 注意：原名 activeSpies，这里可能我有笔误，请检查你的 WorldComponent

        // [修复] 反向引用列表 (ID引用)
        public List<SpyData> activeSpies = new List<SpyData>();

        public FactionSpyData() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref infiltrationPoints, "infiltrationPoints", 0f);
            Scribe_Values.Look(ref controlStatus, "controlStatus", FactionControlStatus.Independent);

            Scribe_References.Look(ref leaderOfficial, "leaderOfficial");
            Scribe_Collections.Look(ref allOfficials, "allOfficials", LookMode.Deep);
            Scribe_Collections.Look(ref activeSpies, "activeSpies", LookMode.Reference);
        }

        /// <summary>
        /// 获取当前的渗透等级 (0-5)
        /// Lv0: 0-19 (未知)
        /// Lv1: 20-39 (初步接触)
        /// Lv2: 40-59 (建立网络)
        /// Lv3: 60-79 (深度渗透)
        /// Lv4: 80-99 (核心掌控)
        /// Lv5: 100   (完全支配)
        /// </summary>
        public int InfiltrationLevel
        {
            get
            {
                if (infiltrationPoints >= 100f - float.Epsilon) return 5;
                if (infiltrationPoints >= 80f) return 4;
                if (infiltrationPoints >= 60f) return 3;
                if (infiltrationPoints >= 40f) return 2;
                if (infiltrationPoints >= 20f) return 1;
                return 0;
            }
        }
    }
}