using System;
using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Race
{
    public class CompProperties_RavenRace : CompProperties
    {
        public CompProperties_RavenRace()
        {
            this.compClass = typeof(CompRavenRace);
        }
    }

    /// <summary>
    /// 渡鸦族核心组件
    /// 职责：
    /// 1. 初始化种族特有的能力（如强制求爱）
    /// 2. 验证 Facial Animation 是否正确加载
    /// </summary>
    public class CompRavenRace : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeAbilities();

            // --- Facial Animation 调试检查 ---
            // 仅在生成时检查一次，确认组件是否注入成功
            if (!respawningAfterLoad && IsFacialAnimationActive())
            {
                CheckFAComponents();
            }
        }

        private void CheckFAComponents()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn == null) return;

            // 检查是否拥有 FA 的核心组件 HeadControllerComp
            bool hasFA = pawn.AllComps.Any(c => c.GetType().Name == "HeadControllerComp");

            if (!hasFA)
            {
                Log.Error($"[RavenRace] 严重错误: 检测到 Facial Animation 已启用，但渡鸦族({pawn.Name})身上缺少 FA 组件！请检查 Patch_Raven_FA_Components.xml 是否生效。");
            }
            else
            {
                if (RavenRaceMod.Settings.enableDebugMode)
                {
                    Log.Message($"[RavenRace] FA 组件检查通过: {pawn.Name} 已成功加载面部动画组件。");
                }
            }
        }

        private bool IsFacialAnimationActive()
        {
            return ModsConfig.IsActive("Nals.FacialAnimation") || ModsConfig.IsActive("2850854272");
        }

        /// <summary>
        /// 确保Pawn拥有种族固有的能力
        /// </summary>
        private void InitializeAbilities()
        {
            Pawn pawn = this.parent as Pawn;
            if (pawn == null || pawn.abilities == null) return;

            // 只有成年且有生理能力时才获得
            if (!pawn.ageTracker.Adult) return;

            AbilityDef forceLovinDef = RavenDefOf.Raven_Ability_ForceLovin;
            if (forceLovinDef == null) return;

            if (pawn.abilities.GetAbility(forceLovinDef) == null)
            {
                pawn.abilities.GainAbility(forceLovinDef);
            }
        }
    }
}