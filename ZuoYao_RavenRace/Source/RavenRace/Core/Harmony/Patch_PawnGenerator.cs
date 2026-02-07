using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;
using AlienRace; // 必须引用 HAR 的命名空间

namespace RavenRace
{
    /// <summary>
    /// 强制渡鸦族肤色为白色的补丁
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __result)
        {
            // 1. 基础检查
            if (__result == null) return;

            // 2. 检查是否为渡鸦族
            // [修改] 使用 DefOf 进行对象比较，比字符串比较更快且防拼写错误
            if (__result.def == RavenDefOf.Raven_Race)
            {
                ForceWhiteSkin(__result);
            }
        }

        /// <summary>
        /// 强制将 Pawn 的皮肤颜色设置为纯白
        /// </summary>
        public static void ForceWhiteSkin(Pawn pawn)
        {
            if (pawn == null) return;

            // 1. 获取 HAR 的组件
            var alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
            if (alienComp != null)
            {
                // 强制覆写 "skin" 通道为白色
                // OverwriteColorChannel 是 HAR 提供的直接修改颜色的方法
                alienComp.OverwriteColorChannel("skin", Color.white, Color.white);
            }

            // 2. 同时也修正原版的 Story 颜色 (作为双重保险，虽然 HAR 通常会覆盖这个)
            if (pawn.story != null)
            {
                pawn.story.skinColorOverride = Color.white;
            }

            // 3. 如果已有 Melanin (黑色素) 基因，强制移除或修改 (防止基因面板显示不一致)
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                // 查找所有黑色素基因
                var melaninGenes = pawn.genes.GenesListForReading
                    .Where(g => g.def.endogeneCategory == EndogeneCategory.Melanin)
                    .ToList();

                foreach (var gene in melaninGenes)
                {
                    pawn.genes.RemoveGene(gene);
                }
            }

            // 4. 刷新渲染
            if (pawn.Drawer != null && pawn.Drawer.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }
}