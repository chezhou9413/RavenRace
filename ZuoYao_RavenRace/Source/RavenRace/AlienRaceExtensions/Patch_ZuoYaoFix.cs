using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace RavenRace.AlienRaceExtensions
{
    /// <summary>
    /// 存档修复补丁：
    /// 当左爻生成或加载时，强制检查并修正她的外观（发型、衣服），确保专属外观生效。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_ZuoYaoFix
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || __instance.Destroyed) return;

            // 检查是否为左爻 (通过 PawnKind 判断)
            // 务必确保 RavenDefOf.Raven_PawnKind_ZuoYao 已定义
            if (__instance.kindDef == RavenDefOf.Raven_PawnKind_ZuoYao)
            {
                FixZuoYaoAppearance(__instance);
            }
        }

        private static void FixZuoYaoAppearance(Pawn p)
        {
            // 1. 强制发型
            HairDef zuoYaoHair = DefDatabase<HairDef>.GetNamedSilentFail("Raven_Hair_ZuoYao");
            if (zuoYaoHair != null && p.story.hairDef != zuoYaoHair)
            {
                p.story.hairDef = zuoYaoHair;
                p.Drawer.renderer.SetAllGraphicsDirty();
            }

            // 2. 强制服装
            // 定义专属服装 DefName
            string apparelDefName = "Raven_Apparel_ZuoYao_Outfit";
            ThingDef apparelDef = DefDatabase<ThingDef>.GetNamedSilentFail(apparelDefName);

            if (apparelDef != null && p.apparel != null)
            {
                // 检查是否已穿戴
                bool isWearing = p.apparel.WornApparel.Any(x => x.def == apparelDef);

                if (!isWearing)
                {
                    // 移除身上所有冲突层级的衣服 (Shell, OnSkin)
                    List<Apparel> toRemove = new List<Apparel>();
                    foreach (var app in p.apparel.WornApparel)
                    {
                        if (app.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) ||
                            app.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin))
                        {
                            toRemove.Add(app);
                        }
                    }

                    // 执行移除
                    foreach (var app in toRemove)
                    {
                        // 销毁旧衣服，或者丢在地上 (这里选择销毁以保持整洁)
                        app.Destroy();
                    }

                    // 生成并穿上新衣服
                    Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef);
                    p.apparel.Wear(newApparel, false, true); // forced = true
                }
            }

            // 3. 强制刷新渲染树 (解决眼镜等 BodyAddon 不显示的问题)
            p.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}