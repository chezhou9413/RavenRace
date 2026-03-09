using RimWorld;
using Verse;

namespace RavenRace
{
    /// <summary>
    /// 模组全局DefOf类。
    /// 存储了整个模组所有通过XML定义的Def的静态引用。
    /// 内容已按照功能模块进行分类，便于查找和维护。
    /// </summary>
    [DefOf]
    public static class RavenDefOf
    {
        // ===================================================
        // Core & Global - 核心与全局定义
        // ===================================================

        public static ThingDef Raven_Race;                   // 渡鸦种种族定义
        public static PawnKindDef Raven_Colonist;            // 渡鸦族基础PawnKind
        public static FactionDef Fusang_Hidden;              // 扶桑隐藏派系
        public static FactionDef Raven_PlayerFaction;        // 渡鸦族玩家派系

        // ===================================================
        // Features: Bloodline - 血脉
        // ===================================================

        public static HediffDef Raven_Hediff_WallBloodline;      // 墙之血脉
        public static HediffDef Raven_Hediff_MechanoidBloodline; // 机械血脉


        // ===================================================
        // Features: BloodlineRitual - 血脉仪式
        // ===================================================

        public static ResearchProjectDef RavenResearch_BloodlineRegression; // 血脉回溯研究
        public static JobDef Raven_Job_BloodlineRitual;     // 血脉仪式工作


        // ===================================================
        // Features: Purification - 纯化
        // ===================================================

        public static JobDef Raven_Job_PurificationRitual;  // 纯化仪式测试工作



        // ===================================================
        // Features: Soul Altar - 育生祭坛
        // ===================================================

        // 原始摇篮相关
        public static ThingDef Raven_PrimitiveCradle;       // 原始摇篮
        public static JobDef Raven_Job_PlaceEggInCradle;    // 放置灵卵工作

        // 扶桑育生祭坛相关
        public static HediffDef Raven_Hediff_SoulAltarBonus;   // 祭坛赐福
        public static JobDef Raven_Job_FillAltar;           // 填充祭坛工作


        // ===================================================
        // Features: Reproduction - 繁殖系统
        // ===================================================

        // 繁衍至上文化相关 (新增)
        public static MemeDef Raven_Heritage;                    // 繁衍至上模因
        public static RecordDef Raven_Record_LovinCount;         // 交配总数统计
        public static HediffDef Raven_Hediff_ReproductionLust;   // 繁衍渴望Hediff


        //灵卵相关
        public static ThingDef Raven_SpiritEgg;                 // 受精灵卵
        public static ThingDef Raven_SpiritEgg_Unfertilized;    // 未受精灵卵
        public static HediffDef Raven_Hediff_RavenPregnancy;     // 灵卵孕育状态
        public static HediffDef Raven_Hediff_SpiritEggInserted;  // 灵卵填充状态
        public static HediffDef Raven_Hediff_RapidOvulation;     // 催产素强制排卵状态
        public static ThoughtDef Raven_Thought_EggFilled;          // 被灵卵填充心情
        public static JobDef Raven_Job_InsertSpiritEgg;     // 置入灵卵工作
        public static JobDef Raven_Job_RemoveSpiritEgg;     // 取出灵卵工作

        //强制求爱技能
        public static JobDef Raven_Job_ForceLovin;          // 强制求爱工作
        public static AbilityDef Raven_Ability_ForceLovin;          // 强制求爱技能
        public static ThoughtDef Raven_Thought_ForceLovin_Initiator; // 强制求爱 (发起者)心情
        public static ThoughtDef Raven_Thought_ForceLovin_Recipient; // 强制求爱 (接受者)心情


        // ===================================================
        // Features: FusangOrganization - 其实主要是扶桑电台
        // ===================================================

        public static ThingDef Raven_FusangRadio;           // 扶桑电台
        public static JobDef Raven_Job_UseFusangRadio;      // 使用电台工作


        // ===================================================
        // Features: Defense System - 防御系统
        // ===================================================

        public static ThingDef RavenDecoy_Dummy;                // 诱饵假人
        public static ThingDef Raven_TrapWall;                  // 升墙陷阱生成的墙
        public static ThingDef RavenGas_Anesthetic;             // 麻醉气体
        public static ThingDef RavenGas_Aphrodisiac;            // 催情气体
        public static ThingDef RavenTrap_Lego;                  // 乐高陷阱
        public static ThingDef RavenTrap_Conception;            // 受孕陷阱
        public static ThingDef RavenTrap_RisingWall;            // 升墙陷阱
        public static ThingDef RavenGas_BlackMist;              // 黑灵迷雾
        public static ThingDef RavenTrap_Beast;                 // 捕兽夹

        public static HediffDef RavenHediff_TendonCut;          // 跟腱断裂Hediff
        public static HediffDef RavenHediff_AnestheticBuildup;  // 麻醉累积Hediff
        public static HediffDef RavenHediff_AphrodisiacEffect;  // 催情效果Hediff
        public static HediffDef RavenHediff_LegoPain;           // 乐高之痛Hediff
        public static HediffDef RavenHediff_ConceptionProcess;  // 受孕过程Hediff

        public static JobDef Raven_Job_EnterConcealment;        // 进入掩体工作


        // ===================================================
        // Features: UniqueEquipment & UniqueWeapons - 独特装备和武器
        // ===================================================

        // 灵卵拉珠
        public static ThingDef Raven_Weapon_SpiritBeads;        // 物品定义
        public static JobDef Raven_Job_InsertBeads;             // 纳珠工作
        public static HediffDef Raven_Hediff_SpiritBeadsInserted; // 纳珠状态
        public static HediffDef Raven_Hediff_HighClimax;          // 绝顶高潮状态
        public static AbilityDef Raven_Ability_GrandClimax;       // 盛大高潮技能

        // 暗影斗篷
        public static ThingDef Raven_Apparel_ShadowCloak;       // 物品定义
        public static HediffDef Raven_Hediff_ShadowStep;         // 影步状态
        public static HediffDef Raven_Hediff_ShadowAttackCooldown; // 暗影冷却状态

        // ===================================================
        // Features: Custom Pawns - 特殊角色
        // ===================================================

        //左爻
        public static PawnKindDef Raven_PawnKind_ZuoYao;          // 左爻PawnKind
        public static BackstoryDef Raven_Backstory_ZuoYao_Child;  // 左爻童年背景
        public static BackstoryDef Raven_Backstory_ZuoYao_Adult;   // 左爻成年背景
        public static TraitDef Raven_Trait_ZuoYao;              // 左爻特性

        public static AbilityDef Raven_Ability_Kotoamatsukami;     // 别天神技能
        public static PawnRelationDef Raven_Relation_AbsoluteMaster; // 绝对主人关系
        public static PawnRelationDef Raven_Relation_LoyalServant;   // 忠诚仆人关系
        public static ThoughtDef Raven_Thought_Kotoamatsukami_Recruited; // 别天神招募心情


        //Binah
        [MayRequire("Chezhou.ChezhouLib.lib")]
        public static PawnKindDef Raven_PawnKind_Binah;             // Binah PawnKind


        // ===================================================
        // Features: Creatures - 特殊动物
        // ===================================================
        public static PawnKindDef Raven_HighArchon;               // 渡鸦大统领PawnKind
        public static HediffDef Raven_Hediff_GoldenSpirit;        // 黄金精神Hediff

        // ===================================================
        // Features: DegradationCharm - 淫堕符咒
        // ===================================================

        public static ThingDef Raven_Item_CorruptionTalisman;   // 淫堕符咒物品
        public static HediffDef Raven_Hediff_Degradation;      // 淫堕刻印Hediff
        public static TraitDef Raven_Trait_Lecherous;         // 淫堕狂宴特性
        public static JobDef Raven_Job_ApplyCharm;            // 贴符工作
        public static JobDef Raven_Job_RemoveCharm;           // 揭符工作

        // ===================================================
        // Features: Bionics- 仿生体
        // ===================================================

        public static ThingDef Filth_RavenBodilyFluid;          // 特殊体液污渍
        public static HediffDef Raven_Hediff_FluidAccelerator;     // 液体促进器Hediff


        // ===================================================
        // Features: Hypnosis - 催眠App
        // ===================================================
        public static ThingDef Raven_Item_HypnosisApp;
        public static JobDef Raven_Job_HypnoticSelfPleasure;
        public static InteractionDef Raven_Interaction_HypnosisCmd;

        // ===================================================
        // Features: Mechanical Angel - 堕落的机械天使
        // ===================================================
        public static ThingDef Raven_Mech_Aegis;
        public static JobDef Raven_Job_AegisLustCharge;
        public static HediffDef Raven_Hediff_AegisDrained;
        public static ThoughtDef Raven_Thought_AegisDrained;
        public static ThingDef Raven_Building_AngelGestator; // 神骸堕化调教舱
        public static HediffDef Raven_Hediff_AegisPanacea;
        public static ThoughtDef Raven_Thought_AegisPanacea;
        public static HediffDef Raven_Hediff_AegisRampage;
        public static JobDef Raven_Job_AegisRampageCharge;





        // ===================================================
        // Features: MiscSmallFeatures - 不好分类的小Feature
        // ===================================================

        //飞机杯相关
        public static ThingDef Raven_Item_MasturbatorCup;       // 飞机杯物品
        public static JobDef Raven_Job_MasturbateWithCup;       // 使用飞机杯工作
        public static JobDef Raven_Job_DimensionalClimax;      // 次元高潮工作
        public static ThoughtDef Raven_Thought_MasturbatedWithCup; // 使用飞机杯心情

        //看AV相关
        public static ThoughtDef Raven_Thought_WatchedAV;     // 观看AV心情
        public static JoyKindDef Raven_AdultEntertainment;    // 成人娱乐类型

        //AV摄影系统
        public static RoomRoleDef Raven_RoomRole_AVStudio;    // AV摄影房
        public static ThingDef Raven_Building_AVCamera;       // AV摄影机
        public static ThingDef Raven_Item_AVRecord;           // 普通录像带
        public static ThingDef Raven_Item_AVRecord_Premium;   // 典藏录像带

        // 黏液浴缸相关
        public static ThingDef Raven_Building_SlimeBathtub; // 黏液浴缸
        public static JobDef Raven_Job_TakeSlimeBath;       // 泡澡工作
        public static JoyKindDef Raven_SlimeBathJoy;          // 泡澡娱乐类型
        public static ThoughtDef Raven_Thought_SlimeBath;     // 泡澡后心情

        // 吸星大法相关
        public static AbilityDef Raven_Ability_DevourPawn;          // 吸星大法技能
        public static ThingDef Raven_Projectile_DevourPull;       // 吸星大法牵引抛射物
        public static HediffDef Raven_Hediff_DevouredPawnHolder;    // 胎内牢笼Hediff

        // 暖床 (我想把这个也放到MiscSmallFeatures下）
        public static ThoughtDef Raven_Thought_Snuggle;             // 暖床 (个人心情)

        // 忏悔室相关 [新增]
        public static ThingDef Raven_Building_ConfessionBooth;
        public static JobDef Raven_Job_EnterConfessionBooth;
        public static HediffDef Raven_Hediff_PurifiedByLust;
        public static HediffDef Raven_Hediff_NunReceptacle;
        public static ThoughtDef Raven_Thought_ConfessedSin;
        public static ThoughtDef Raven_Thought_AbsorbedSin;


        // ===================================================
        // Features: Ideology & Culture - 文化与风格
        // ===================================================

        public static StyleCategoryDef Raven_StyleCategory;      // 渡鸦风格分类
        public static IdeoIconDef Raven_IdeoIcon;                // 渡鸦文化符号图标

        // 戒律引用 (用于逻辑检查，虽然大部分在XML处理)
        public static PreceptDef Lovin_FreeApproved;             // 肉欲：推崇
        public static PreceptDef Corpses_DontCare;               // 尸体：漠视


        // ===================================================
        // Features: Servitude - 侍奉系统
        // ===================================================
        public static InteractionDef Raven_Interaction_Seduce;//色诱
        public static ThoughtDef Raven_Thought_HasServant;//有仆人想法
        public static ThoughtDef Raven_Thought_IsServant;//有主人想法

        //各项侍奉互动
        public static JobDef Raven_Job_FollowMaster;//跟随
        public static JobDef Raven_Job_CleanseMaster;//擦拭身体
        public static JobDef Raven_Job_LapPillow;//膝枕
        public static JobDef Raven_Job_FeedMaster;//喂食


        // ===================================================
        // Others - 其他
        // ===================================================


        public static HediffDef Raven_Hediff_EmberDrain;            // 余烬枯竭Hediff

        public static ThingDef Raven_Weapon_HiddenBlade;        // 袖剑
        public static HediffDef Raven_Hediff_HiddenBladePrep;    // 袖剑准备状态
        public static HediffDef Raven_Hediff_SteadyAim;          // 稳定瞄准状态

        public static PawnRelationDef Raven_Relation_Dominated; // 支配关系

        public static ThingDef Raven_GoldenFeather;             // 折翼金羽
        public static ThingDef RavenItem_SevenEmotionsNectar;   // 七情花蜜
        public static ThingDef Raven_Item_Suppository;          // 渡鸦栓剂
        public static ThingDef Raven_Item_MiracleHeal;          // 特效治疗药
        public static ThingDef RavenItem_AbilityTome;           // 技能教本

        public static ThingDef Raven_Item_HojoCube;             // 齁金魔方

        public static ThoughtDef Raven_Thought_IncenseSmell;  // 催情香气心情

        public static HediffDef RavenHediff_IncenseAura;      // 香薰氛围Hediff





        // ===================================================
        // 兼容性 (Compatibility)
        // ===================================================
        [MayRequire("HAR.MuGirlRace")] // [修正] 之前是Nukafrog.MooGirl，根据您的XML应为HAR.MuGirlRace
        public static AbilityDef Raven_Ability_MuGirlCharge;

        /// <summary>
        /// 静态构造函数，确保DefOf在游戏加载时被正确初始化。
        /// </summary>
        static RavenDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RavenDefOf));
        }
    }
}