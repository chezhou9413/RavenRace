using RimWorld;
using Verse;

namespace RavenRace
{
    [DefOf]
    public static class RavenDefOf
    {
        // --- 种族与生物 ---
        public static ThingDef Raven_Race;
        public static PawnKindDef Raven_Colonist;
        public static PawnKindDef Raven_PawnKind_ZuoYao;
        public static PawnKindDef Raven_HighArchon; // 确保有这个引用

        // --- 物品 & 建筑 ---
        public static ThingDef Raven_SpiritEgg;
        public static ThingDef Raven_SpiritEgg_Unfertilized;
        public static ThingDef Raven_EmberBlood;
        public static ThingDef Raven_PrimitiveCradle;
        public static ThingDef Raven_Item_Suppository;
        public static ThingDef Raven_Item_MiracleHeal;
        public static ThingDef Raven_GoldenFeather;

        public static ThingDef RavenItem_SevenEmotionsNectar;        // 七情花蜜

        public static ThingDef Raven_Item_MasturbatorCup;

        public static ThingDef Raven_Item_HojoCube;        // 齁金魔方

        public static ThingDef RavenItem_AbilityTome;        // 技能教本

        // --- 武器 & 装备 ---
        public static ThingDef Raven_Weapon_HiddenBlade;
        public static ThingDef Raven_Weapon_SpiritBeads;
        public static ThingDef Raven_Apparel_ShadowCloak;

        // --- 气体 & 陷阱 ---
        public static ThingDef RavenGas_BlackMist;
        public static ThingDef RavenGas_Anesthetic;
        public static ThingDef RavenGas_Aphrodisiac;
        public static ThingDef RavenDecoy_Dummy;

        // --- 能力 (Abilities) ---
        public static AbilityDef Raven_Ability_ForceLovin;
        public static AbilityDef Raven_Ability_Kotoamatsukami;
        public static AbilityDef Raven_Ability_GrandClimax;

        [MayRequire("Nukafrog.MooGirl")]
        public static AbilityDef Raven_Ability_MuGirlCharge;

        // --- 工作 (Jobs) ---
        public static JobDef Raven_Job_ForceLovin;
        public static JobDef Raven_Job_PlaceEggInCradle;
        public static JobDef Raven_Job_InsertSpiritEgg;
        public static JobDef Raven_Job_RemoveSpiritEgg;
        public static JobDef Raven_Job_FillAltar;
        public static JobDef Raven_Job_BloodlineRitual;
        public static JobDef Raven_Job_InsertBeads;
        public static JobDef Raven_Job_UseFusangRadio;
        public static JobDef Raven_Job_EnterConcealment;
        public static JobDef Raven_Job_ApplyCharm;
        public static JobDef Raven_Job_RemoveCharm;

        public static JobDef Raven_Job_MasturbateWithCup;
        public static JobDef Raven_Job_DimensionalClimax;

        // --- 状态 (Hediffs) ---
        public static HediffDef Raven_Hediff_EmberDrain;
        public static HediffDef Raven_Hediff_RavenPregnancy;
        public static HediffDef Raven_Hediff_SpiritEggInserted;
        public static HediffDef Raven_Hediff_RapidOvulation;
        public static HediffDef Raven_Hediff_SoulAltarBonus;

        public static HediffDef Raven_Hediff_SpiritBeadsInserted;
        public static HediffDef Raven_Hediff_HighClimax;
        public static HediffDef Raven_Hediff_HiddenBladePrep;
        public static HediffDef Raven_Hediff_SteadyAim;
        public static HediffDef Raven_Hediff_ShadowStep;
        public static HediffDef Raven_Hediff_ShadowAttackCooldown;
        public static HediffDef Raven_Hediff_Degradation;
        public static HediffDef RavenHediff_AphrodisiacEffect;
        public static HediffDef RavenHediff_AnestheticBuildup;
        public static HediffDef RavenHediff_LegoPain;
        public static HediffDef RavenHediff_TendonCut;
        public static HediffDef RavenHediff_ConceptionProcess;
        public static HediffDef RavenHediff_IncenseAura;

        public static HediffDef Raven_Hediff_GoldenSpirit;        // 黄金精神

        // --- 思想 (Thoughts) ---
        public static ThoughtDef Raven_Thought_ForceLovin_Initiator;
        public static ThoughtDef Raven_Thought_ForceLovin_Recipient;
        public static ThoughtDef Raven_Thought_EggFilled;
        public static ThoughtDef Raven_Thought_Kotoamatsukami_Recruited;
        public static ThoughtDef Raven_Thought_Snuggle;
        public static ThoughtDef Raven_Thought_IncenseSmell;
        public static ThoughtDef Raven_Thought_WatchedAV;

        public static ThoughtDef Raven_Thought_MasturbatedWithCup;

        public static JoyKindDef Raven_AdultEntertainment;

        // --- 关系 ---
        public static PawnRelationDef Raven_Relation_Dominated;
        public static PawnRelationDef Raven_Relation_AbsoluteMaster;
        public static PawnRelationDef Raven_Relation_LoyalServant;

        // --- 故事与背景 ---
        public static BackstoryDef Raven_Backstory_ZuoYao_Child;
        public static BackstoryDef Raven_Backstory_ZuoYao_Adult;
        public static TraitDef Raven_Trait_ZuoYao;
        public static TraitDef Raven_Trait_Lecherous;

        // --- 研究 ---
        public static ResearchProjectDef RavenResearch_BloodlineRegression;

        // --- 派系 ---
        public static FactionDef Fusang_Hidden;

        static RavenDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RavenDefOf));
        }
    }
}