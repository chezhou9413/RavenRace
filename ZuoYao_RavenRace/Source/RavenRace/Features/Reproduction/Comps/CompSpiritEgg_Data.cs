using System.Collections.Generic;
using Verse;
using RimWorld;

// [关键] 必须与主文件保持一致的命名空间
namespace RavenRace.Features.Reproduction
{
    public partial class CompSpiritEgg
    {
        // ==========================================
        // 数据字段
        // ==========================================
        public string fatherId;
        public string motherId;
        public string fatherName;
        public string motherName;
        public Faction faction;

        public PawnKindDef pawnKind;
        public GeneSet geneSet;
        public string xenotypeName;
        public XenotypeIconDef iconDef;

        public Dictionary<string, float> bloodlineComposition = new Dictionary<string, float>();
        public float goldenCrowConcentration;

        private float progress = 0f;
        public bool isIncubating = false;
        public List<string> storedUpgradeDefNames = new List<string>();

        // 温养进度 (0.0 ~ 1.0)
        public float warmthProgress = 0f;

        public float Progress => progress;

        public float TotalTicksNeeded
        {
            get
            {
                float days = 15f;
                if (RavenRaceMod.Settings != null)
                {
                    days = System.Math.Max(0.1f, RavenRaceMod.Settings.baseHatchingDays);
                }
                return days * 60000f;
            }
        }

        // [关键] Override 必须在继承了 ThingComp 的上下文中才有效
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref motherId, "motherId");
            Scribe_Values.Look(ref fatherId, "fatherId");
            Scribe_Values.Look(ref motherName, "motherName");
            Scribe_Values.Look(ref fatherName, "fatherName");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Deep.Look(ref geneSet, "geneSet");
            Scribe_Collections.Look(ref bloodlineComposition, "bloodlineComposition", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref goldenCrowConcentration, "goldenCrowConcentration");
            Scribe_Values.Look(ref progress, "progress", 0f);
            Scribe_Values.Look(ref isIncubating, "isIncubating", false);

            Scribe_Values.Look(ref warmthProgress, "warmthProgress", 0f);

            Scribe_Collections.Look(ref storedUpgradeDefNames, "storedUpgradeDefNames", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (bloodlineComposition == null) bloodlineComposition = new Dictionary<string, float>();
                if (storedUpgradeDefNames == null) storedUpgradeDefNames = new List<string>();
            }
        }
    }
}