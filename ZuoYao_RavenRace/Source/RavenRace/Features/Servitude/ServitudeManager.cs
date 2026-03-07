using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace RavenRace.Features.Servitude
{
    // 用于XML定义的ModExtension
    public class ServitudeDefExtension : DefModExtension
    {
        public List<ServitudeInteraction> interactions = new List<ServitudeInteraction>();
    }

    // 单个互动的数据结构
    public class ServitudeInteraction : IExposable
    {
        public JobDef jobDef;
        public FleckDef fleckDef;
        public string letterLabel;
        public string letterText;
        public float baseChance = 0.1f;
        public int cooldownTicks = 15000;
        public JobDef requiredMasterJobDef;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref jobDef, "jobDef");
            Scribe_Defs.Look(ref fleckDef, "fleckDef");
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref letterText, "letterText");
            Scribe_Values.Look(ref baseChance, "baseChance", 0.1f);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 15000);
            Scribe_Defs.Look(ref requiredMasterJobDef, "requiredMasterJobDef");
        }
    }

    // 主管理器
    public class ServitudeManager : WorldComponent
    {
        // 核心数据结构：奴隶 -> 主人 (一对一关系)，序列化存储它足够了
        private Dictionary<Pawn, Pawn> servantToMaster = new Dictionary<Pawn, Pawn>();

        // 运行时结构：主人 -> 奴隶列表 (一主多奴关系)，会在加载时动态重建
        private Dictionary<Pawn, List<Pawn>> masterToServants = new Dictionary<Pawn, List<Pawn>>();

        private Dictionary<int, int> interactionCooldowns = new Dictionary<int, int>();

        // 存档兼容变量 (用于读取旧版本的 masterToServant)
        private Dictionary<Pawn, Pawn> legacy_masterToServant;

        public ServitudeManager(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            // 核心存储
            Scribe_Collections.Look(ref servantToMaster, "servantToMaster", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref interactionCooldowns, "interactionCooldowns", LookMode.Value, LookMode.Value);

            // 读取老存档的数据，以防止丢失
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Collections.Look(ref legacy_masterToServant, "masterToServant", LookMode.Reference, LookMode.Reference);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                servantToMaster ??= new Dictionary<Pawn, Pawn>();
                interactionCooldowns ??= new Dictionary<int, int>();
                masterToServants = new Dictionary<Pawn, List<Pawn>>();

                // 老存档兼容数据迁移
                if (legacy_masterToServant != null)
                {
                    foreach (var kvp in legacy_masterToServant)
                    {
                        if (kvp.Key != null && kvp.Value != null && !servantToMaster.ContainsKey(kvp.Value))
                        {
                            // 旧版是 master(key) -> servant(value)，翻转存入新架构
                            servantToMaster[kvp.Value] = kvp.Key;
                        }
                    }
                    legacy_masterToServant = null; // 释放内存
                }

                // 基于 servantToMaster 动态重建 masterToServants
                foreach (var kvp in servantToMaster)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        if (!masterToServants.ContainsKey(kvp.Value))
                        {
                            masterToServants[kvp.Value] = new List<Pawn>();
                        }
                        masterToServants[kvp.Value].Add(kvp.Key);
                    }
                }
                Cleanup();
            }
        }

        public static ServitudeManager Get() => Current.Game.World.GetComponent<ServitudeManager>();

        /// <summary>
        /// 为主人添加一名奴隶
        /// </summary>
        public void AddRelation(Pawn master, Pawn servant)
        {
            if (master == null || servant == null || master == servant) return;

            // 一个奴隶只能有一个主人，先解除它原有的服侍
            RemoveRelation(servant);

            servantToMaster[servant] = master;

            if (!masterToServants.ContainsKey(master))
            {
                masterToServants[master] = new List<Pawn>();
            }
            if (!masterToServants[master].Contains(servant))
            {
                masterToServants[master].Add(servant);
            }

            Messages.Message($"{servant.LabelShort} 现在开始侍奉 {master.LabelShort}。", servant, MessageTypeDefOf.PositiveEvent);
            Cleanup();
        }

        /// <summary>
        /// 专门用于解除某个特定奴隶的主从关系
        /// </summary>
        public void RemoveRelation(Pawn servant)
        {
            if (servant != null && servantToMaster.TryGetValue(servant, out Pawn master))
            {
                servantToMaster.Remove(servant);

                if (masterToServants.ContainsKey(master))
                {
                    masterToServants[master].Remove(servant);
                    if (masterToServants[master].Count == 0)
                    {
                        masterToServants.Remove(master);
                    }
                }
                Messages.Message($"{servant.LabelShort} 解除了与 {master.LabelShort} 的主从关系。", servant, MessageTypeDefOf.NeutralEvent);
            }
        }

        /// <summary>
        /// 一键解除某位主人名下的所有奴隶
        /// </summary>
        public void RemoveAllServants(Pawn master)
        {
            if (master != null && masterToServants.TryGetValue(master, out List<Pawn> servants))
            {
                // 使用 ToList 创建副本，防止在遍历时修改原集合导致枚举异常
                foreach (var s in servants.ToList())
                {
                    RemoveRelation(s);
                }
            }
        }

        public bool IsMaster(Pawn pawn) => pawn != null && masterToServants.ContainsKey(pawn) && masterToServants[pawn].Count > 0;
        public bool IsServant(Pawn pawn) => pawn != null && servantToMaster.ContainsKey(pawn);

        public Pawn GetMaster(Pawn servant) => servantToMaster.TryGetValue(servant, out var master) ? master : null;
        public List<Pawn> GetServants(Pawn master) => masterToServants.TryGetValue(master, out var list) ? list : new List<Pawn>();

        public bool IsOnCooldown(Pawn servant, JobDef jobDef)
        {
            int key = Gen.HashCombine(servant.thingIDNumber, jobDef.GetHashCode());
            return interactionCooldowns.TryGetValue(key, out int expiryTick) && Find.TickManager.TicksGame < expiryTick;
        }

        public void StartCooldown(Pawn servant, ServitudeInteraction interaction)
        {
            int key = Gen.HashCombine(servant.thingIDNumber, interaction.jobDef.GetHashCode());
            interactionCooldowns[key] = Find.TickManager.TicksGame + interaction.cooldownTicks;
        }

        private void Cleanup()
        {
            // 清理已死亡或被移除的引用
            var invalidServants = servantToMaster.Keys.Where(p => p.DestroyedOrNull()).ToList();
            foreach (var invalid in invalidServants)
            {
                RemoveRelation(invalid); // 安全剥离
            }

            var invalidMasters = masterToServants.Keys.Where(p => p.DestroyedOrNull()).ToList();
            foreach (var invalid in invalidMasters)
            {
                RemoveAllServants(invalid);
            }
        }
    }
}