using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RavenRace.Features.RavenRite.Rite_Promotion.Purification.Defs
{
    /// <summary>
    /// 血脉纯化阶段定义 (核心蓝图)。
    /// 可以在XML中灵活配置渡鸦在不同的金乌血脉浓度阶段所受到的限制与获得的奖励。
    /// </summary>
    public class PurificationStageDef : Def
    {
        /// <summary>
        /// 阶段索引。0代表初始状态（还没突破任何极限），1代表一阶突破。
        /// </summary>
        public int stageIndex = 0;

        /// <summary>
        /// 处在该阶段的渡鸦，金乌浓度的物理上限。
        /// </summary>
        public float concentrationThreshold = 1.0f;

        // --- 达到及突破此阶段后，将永久赋予的奖励 ---
        public List<HediffDef> grantedHediffs;
        public List<AbilityDef> grantedAbilities;
        public List<TraitDef> grantedTraits;

        // --- 预留字段：未来极其复杂的机制 ---
        /// <summary>
        /// 改变成指定的 PawnKindDef (预留接口，暂未实装C#逻辑)
        /// </summary>
        public PawnKindDef changeToPawnKind;

        /// <summary>
        /// 改变成指定的种族 ThingDef (预留接口，暂未实装C#逻辑)
        /// </summary>
        public ThingDef changeToRace;
    }
}