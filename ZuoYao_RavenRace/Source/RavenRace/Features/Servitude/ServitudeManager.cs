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
        // [错误修复] 最终修正：改回 FleckDef
        public FleckDef fleckDef;
        public string letterLabel;
        public string letterText;
        public float baseChance = 0.1f;
        public int cooldownTicks = 15000;
        public JobDef requiredMasterJobDef;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref jobDef, "jobDef");
            // [错误修复] 最终修正
            Scribe_Defs.Look(ref fleckDef, "fleckDef");
            Scribe_Values.Look(ref letterLabel, "letterLabel");
            Scribe_Values.Look(ref letterText, "letterText");
            Scribe_Values.Look(ref baseChance, "baseChance", 0.1f);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 15000);
            Scribe_Defs.Look(ref requiredMasterJobDef, "requiredMasterJobDef");
        }
    }

    // 主管理器 (此类其余部分无需修改，保持原样)
    public class ServitudeManager : WorldComponent
    {
        private Dictionary<Pawn, Pawn> servantToMaster = new Dictionary<Pawn, Pawn>();
        private Dictionary<Pawn, Pawn> masterToServant = new Dictionary<Pawn, Pawn>();
        private Dictionary<int, int> interactionCooldowns = new Dictionary<int, int>();

        public ServitudeManager(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref servantToMaster, "servantToMaster", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref masterToServant, "masterToServant", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref interactionCooldowns, "interactionCooldowns", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                servantToMaster ??= new Dictionary<Pawn, Pawn>();
                masterToServant ??= new Dictionary<Pawn, Pawn>();
                interactionCooldowns ??= new Dictionary<int, int>();
                Cleanup();
            }
        }

        public static ServitudeManager Get() => Current.Game.World.GetComponent<ServitudeManager>();

        public void AddRelation(Pawn master, Pawn servant)
        {
            if (master == null || servant == null || master == servant) return;
            RemoveRelation(master);
            RemoveRelation(servant);
            masterToServant[master] = servant;
            servantToMaster[servant] = master;
            Messages.Message($"{servant.LabelShort} 现在开始侍奉 {master.LabelShort}。", servant, MessageTypeDefOf.PositiveEvent);
            Cleanup();
        }

        public void RemoveRelation(Pawn pawn)
        {
            if (pawn == null) return;
            if (masterToServant.ContainsKey(pawn))
            {
                Pawn oldServant = masterToServant[pawn];
                masterToServant.Remove(pawn);
                if (oldServant != null) servantToMaster.Remove(oldServant);
                Messages.Message($"{pawn.LabelShort} 解除了与 {oldServant?.LabelShort ?? "侍奉者"} 的主从关系。", pawn, MessageTypeDefOf.NeutralEvent);
            }
            if (servantToMaster.ContainsKey(pawn))
            {
                Pawn oldMaster = servantToMaster[pawn];
                servantToMaster.Remove(pawn);
                if (oldMaster != null) masterToServant.Remove(oldMaster);
                Messages.Message($"{pawn.LabelShort} 停止了对 {oldMaster?.LabelShort ?? "主人"} 的侍奉。", pawn, MessageTypeDefOf.NeutralEvent);
            }
        }

        public bool IsMaster(Pawn pawn) => masterToServant.ContainsKey(pawn);
        public bool IsServant(Pawn pawn) => servantToMaster.ContainsKey(pawn);
        public Pawn GetMaster(Pawn servant) => servantToMaster.TryGetValue(servant, out var master) ? master : null;
        public Pawn GetServant(Pawn master) => masterToServant.TryGetValue(master, out var servant) ? servant : null;

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
            servantToMaster = servantToMaster.Where(kvp => kvp.Key != null && !kvp.Key.DestroyedOrNull() && kvp.Value != null && !kvp.Value.DestroyedOrNull()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            masterToServant = masterToServant.Where(kvp => kvp.Key != null && !kvp.Key.DestroyedOrNull() && kvp.Value != null && !kvp.Value.DestroyedOrNull()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}