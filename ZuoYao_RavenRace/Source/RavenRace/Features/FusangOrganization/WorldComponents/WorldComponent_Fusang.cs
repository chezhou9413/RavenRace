using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using RavenRace.Features.StoryEngine; // 引用 StoryEngine

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
        public int lastSupportTick = -99999;
        public int tradeCooldownTicks = 180000; // 3天

        // [删除] hasContactedBefore, firstContactDialogueStage, completedFirstContact
        // 这些变量现在应该通过 StoryWorldComponent.HasFlag("Fusang_Contacted") 来替代
        // 为了数据迁移的安全性，我们可以在 ExposeData 里读取旧数据并转换，但不保留字段。

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

            // [数据迁移逻辑]
            // 如果是旧存档，尝试读取旧变量并写入新系统
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                bool old_completedFirstContact = false;
                Scribe_Values.Look(ref old_completedFirstContact, "completedFirstContact", false);

                if (old_completedFirstContact)
                {
                    // 将旧标记转换为新系统的 Flag
                    // 注意：这需要在 PostLoadInit 或延迟执行，因为 StoryWorldComponent 可能还没加载
                    // 这里简化处理：直接调用静态 Flag 设置（如果 Component 存在）
                    StoryWorldComponent.SetFlag("Fusang_FirstContact_Complete", true);
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (resources == null) InitializeResources();
            }
        }
    }
}