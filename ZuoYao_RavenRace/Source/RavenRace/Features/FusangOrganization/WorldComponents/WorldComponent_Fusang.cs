using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 扶桑系统的核心数据存储
    /// </summary>
    public class WorldComponent_Fusang : WorldComponent
    {
        // 资源存储
        private Dictionary<FusangResourceType, FusangResource> resources;

        // 冷却计时器 (Tick)
        public int lastTradeTick = -99999;
        public int lastSupportTick = -99999; // 战术支援冷却
        public int tradeCooldownTicks = 180000; // 3天

        // 状态标记
        public bool hasContactedBefore = false;

        // [新增] GALGAME对话系统状态
        public int firstContactDialogueStage = 0; // 当前对话节点ID
        public bool completedFirstContact = false; // 是否已完成初见对话

        public WorldComponent_Fusang(World world) : base(world)
        {
            InitializeResources();
        }

        private void InitializeResources()
        {
            resources = new Dictionary<FusangResourceType, FusangResource>
            {
                { FusangResourceType.Resources, new FusangResource(FusangResourceType.Resources, 50) },
                { FusangResourceType.Military, new FusangResource(FusangResourceType.Military, 10) },
                { FusangResourceType.Intel, new FusangResource(FusangResourceType.Intel, 0) },
                { FusangResourceType.Influence, new FusangResource(FusangResourceType.Influence, 0) }
            };
        }

        public int GetResource(FusangResourceType type)
        {
            if (resources == null) InitializeResources();
            if (resources.TryGetValue(type, out var res)) return res.amount;
            return 0;
        }

        public void ModifyResource(FusangResourceType type, int amount)
        {
            if (resources == null) InitializeResources();
            if (resources.TryGetValue(type, out var res))
            {
                res.amount += amount;
                if (res.amount < 0) res.amount = 0;
                if (res.amount > res.max) res.amount = res.max;
            }
        }

        public bool CanTradeNow()
        {
            return Find.TickManager.TicksGame >= lastTradeTick + tradeCooldownTicks;
        }

        public int GetTicksUntilTrade()
        {
            return (lastTradeTick + tradeCooldownTicks) - Find.TickManager.TicksGame;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref resources, "resources", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref lastTradeTick, "lastTradeTick", -99999);
            Scribe_Values.Look(ref lastSupportTick, "lastSupportTick", -99999);
            Scribe_Values.Look(ref hasContactedBefore, "hasContactedBefore", false);

            // [新增] 保存/加载对话状态
            Scribe_Values.Look(ref firstContactDialogueStage, "firstContactDialogueStage", 0);
            Scribe_Values.Look(ref completedFirstContact, "completedFirstContact", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (resources == null) InitializeResources();
            }
        }
    }
}