using RimWorld;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 渡鸦族核心 Def 引用
    /// 使用 [DefOf] 属性自动绑定 XML 定义，替代 defName 字符串查找，极大提升性能。
    /// </summary>
    [DefOf]
    public static class RavenDefOf
    {
        // --- 种族与生物 ---
        public static ThingDef Raven_Race;
        public static PawnKindDef Raven_Colonist;
        public static PawnKindDef Raven_PawnKind_ZuoYao;

        // --- 物品 & 建筑 ---
        public static ThingDef Raven_SpiritEgg;
        public static ThingDef Raven_SpiritEgg_Unfertilized;
        public static ThingDef Raven_EmberBlood;
        public static ThingDef Raven_PrimitiveCradle;
        public static ThingDef Raven_Item_Suppository;
        public static ThingDef Raven_Item_MiracleHeal;
        public static ThingDef Raven_GoldenFeather;

        // [新增] 飞机杯
        public static ThingDef Raven_Item_MasturbatorCup;

        // --- 武器 & 装备 ---
        public static ThingDef Raven_Weapon_HiddenBlade;
        public static ThingDef Raven_Weapon_SpiritBeads;
        public static ThingDef Raven_Apparel_ShadowCloak;

        // --- 气体 & 陷阱 ---
        public static ThingDef RavenGas_BlackMist;
        public static ThingDef RavenGas_Anesthetic;
        public static ThingDef RavenGas_Aphrodisiac;
        public static ThingDef RavenDecoy_Dummy; // 假人

        // --- 能力 (Abilities) ---
        public static AbilityDef Raven_Ability_ForceLovin;
        public static AbilityDef Raven_Ability_Kotoamatsukami;
        public static AbilityDef Raven_Ability_GrandClimax;

        // [核心修复] 添加 MayRequire 属性，防止在未安装雪牛娘模组时爆红字
        // 这里的字符串必须与 XML 中 MayRequire 的 Mod PackageId 完全一致
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

        // [新增] 飞机杯相关 Job
        public static JobDef Raven_Job_MasturbateWithCup;
        public static JobDef Raven_Job_DimensionalClimax;

        // --- 状态 (Hediffs) ---
        public static HediffDef Raven_Hediff_EmberDrain;
        public static HediffDef Raven_Hediff_RavenPregnancy;
        public static HediffDef Raven_Hediff_SpiritEggInserted;
        public static HediffDef Raven_Hediff_RapidOvulation;
        public static HediffDef Raven_Hediff_SoulAltarBonus;

        // 特殊效果 Hediff
        public static HediffDef Raven_Hediff_SpiritBeadsInserted;
        public static HediffDef Raven_Hediff_HighClimax;
        public static HediffDef Raven_Hediff_HiddenBladePrep;
        public static HediffDef Raven_Hediff_SteadyAim;
        public static HediffDef Raven_Hediff_ShadowStep;
        public static HediffDef Raven_Hediff_ShadowAttackCooldown;
        public static HediffDef Raven_Hediff_Degradation; // 堕落刻印
        public static HediffDef RavenHediff_AphrodisiacEffect;
        public static HediffDef RavenHediff_AnestheticBuildup;
        public static HediffDef RavenHediff_LegoPain;
        public static HediffDef RavenHediff_TendonCut;
        public static HediffDef RavenHediff_ConceptionProcess;
        public static HediffDef RavenHediff_IncenseAura;

        // --- 思想 (Thoughts) ---
        public static ThoughtDef Raven_Thought_ForceLovin_Initiator;
        public static ThoughtDef Raven_Thought_ForceLovin_Recipient;
        public static ThoughtDef Raven_Thought_EggFilled;
        public static ThoughtDef Raven_Thought_Kotoamatsukami_Recruited;
        public static ThoughtDef Raven_Thought_Snuggle;
        public static ThoughtDef Raven_Thought_IncenseSmell;

        // [新增] 飞机杯思想
        public static ThoughtDef Raven_Thought_MasturbatedWithCup;

        // [修复] 娱乐类型定义 (解决 CS0117 错误)
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