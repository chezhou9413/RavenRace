using System;
using System.Linq;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline;

namespace RavenRace.Compat.Mincho
{
    [StaticConstructorOnStartup]
    public static class MinchoCompatUtility
    {
        public static bool IsMinchoActive { get; private set; }
        public static ThingDef MinchoRaceDef { get; private set; }
        public static ThingDef MinchoMintChocolateDef { get; private set; }
        public static HediffDef MinchoBloodlineHediff { get; private set; }

        static MinchoCompatUtility()
        {
            // 根据XML中的DefName判断Mod是否存在
            MinchoRaceDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mincho_ThingDef");
            IsMinchoActive = (MinchoRaceDef != null);

            if (IsMinchoActive)
            {
                MinchoMintChocolateDef = DefDatabase<ThingDef>.GetNamedSilentFail("Mincho_Mintchoco");
                MinchoBloodlineHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_MinchoBloodline");

                if (MinchoMintChocolateDef == null)
                {
                    Log.Warning("[RavenRace] Mincho compatibility is active, but ThingDef 'Mincho_Mintchoco' was not found. Production will be disabled.");
                }

                RavenModUtility.LogVerbose("[RavenRace] Mincho the Mint Choco Slime detected. Compatibility active.");
            }
        }

        /// <summary>
        /// 检查Pawn是否拥有珉巧血脉
        /// </summary>
        public static bool HasMinchoBloodline(CompBloodline comp)
        {
            if (comp == null || comp.BloodlineComposition == null) return false;
            return comp.BloodlineComposition.ContainsKey("Mincho_ThingDef") &&
                   comp.BloodlineComposition["Mincho_ThingDef"] > 0f;
        }

        /// <summary>
        /// 兼容性逻辑主入口
        /// </summary>
        public static void HandleMinchoBloodline(Pawn pawn, bool hasBloodline)
        {
            HandleMinchoBuff(pawn, hasBloodline);
            HandleMinchoProduction(pawn, hasBloodline);
        }

        /// <summary>
        /// 处理由血脉赋予的永久Hediff（极寒抗性）
        /// </summary>
        private static void HandleMinchoBuff(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || MinchoBloodlineHediff == null) return;

            bool hasHediff = pawn.health.hediffSet.HasHediff(MinchoBloodlineHediff);

            if (hasBloodline && !hasHediff)
            {
                pawn.health.AddHediff(MinchoBloodlineHediff);
            }
            else if (!hasBloodline && hasHediff)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MinchoBloodlineHediff);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        /// <summary>
        /// 处理薄荷巧克力生产逻辑 (使用 ThingComp 以显示在左下角信息栏)
        /// </summary>
        private static void HandleMinchoProduction(Pawn pawn, bool hasBloodline)
        {
            if (pawn == null || MinchoMintChocolateDef == null) return;

            // 检查是否已经有我们的生产组件
            bool hasComp = pawn.AllComps.Any(c => c is CompMinchoPassiveProduction);

            if (hasBloodline && !hasComp)
            {
                try
                {
                    // 动态创建并配置属性
                    var props = new CompProperties_MinchoPassiveProduction
                    {
                        resourceDef = MinchoMintChocolateDef,
                        amount = 10,
                        intervalDays = 0.5f, // 半天产一次
                        labelKey = "RavenRace_MinchoChocolateFullness" // 左下角显示的Key
                    };

                    // 初始化组件并添加到Pawn
                    var newComp = new CompMinchoPassiveProduction();
                    newComp.parent = pawn;
                    newComp.Initialize(props);
                    pawn.AllComps.Add(newComp);

                    // 如果Pawn已经生成，可能需要手动触发一次SpawnSetup来初始化数据
                    // 但对于简单的Ticker组件，直接Add通常就足够了
                }
                catch (Exception ex)
                {
                    Log.Error($"[RavenRace] Failed to grant Mincho Chocolate production to {pawn.LabelShort}: {ex}");
                }
            }
            else if (!hasBloodline && hasComp)
            {
                // 如果不再有血脉，则移除组件
                pawn.AllComps.RemoveAll(c => c is CompMinchoPassiveProduction);
            }
        }
    }
}