using Verse;
using RimWorld;

namespace RavenRace.Features.Espionage
{
    /// <summary>
    /// 代表一个正在执行中的间谍任务实例。
    /// </summary>
    public class ActiveMission : IExposable, ILoadReferenceable
    {
        public int uniqueID;
        public EspionageMissionDef def;
        public SpyData spy;
        public Faction targetFaction;
        public OfficialData targetOfficial; // [修复] 现在 OfficialData 实现了 ILoadReferenceable，这行代码合法了

        public int startTick;
        public int durationTicks;

        // 进度 (0-1)
        public float Progress => (float)(Find.TickManager.TicksGame - startTick) / durationTicks;
        public int TicksRemaining => (startTick + durationTicks) - Find.TickManager.TicksGame;

        public ActiveMission() { }

        // [修复] 构造函数不再负责生成 ID，ID 由 WorldComponent 分配
        public ActiveMission(int id, EspionageMissionDef def, SpyData spy, Faction faction, OfficialData official = null)
        {
            this.uniqueID = id;
            this.def = def;
            this.spy = spy;
            this.targetFaction = faction;
            this.targetOfficial = official;
            this.startTick = Find.TickManager.TicksGame;
            this.durationTicks = (int)(def.baseDurationDays * 60000f);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID");
            Scribe_Defs.Look(ref def, "def");
            Scribe_References.Look(ref spy, "spy");
            Scribe_References.Look(ref targetFaction, "targetFaction");
            Scribe_References.Look(ref targetOfficial, "targetOfficial");

            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref durationTicks, "durationTicks");
        }

        public string GetUniqueLoadID() => "ActiveMission_" + uniqueID;
    }
}