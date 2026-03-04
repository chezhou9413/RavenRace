using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RavenRace.Core.Harmony
{
    /// <summary>
    /// 文化图标颜色补丁
    /// 目的：防止渡鸦专属文化图标被强制染成文化颜色（如红色、蓝色），保持贴图原本的色彩。
    /// </summary>
    public static class Patch_IdeoIconColor
    {
        // 获取渡鸦图标的 Def 引用
        private static IdeoIconDef RavenIcon => RavenDefOf.Raven_IdeoIcon;

        /// <summary>
        /// 补丁 1: 拦截 IdeoUIUtility.DoNameAndSymbol
        /// 作用于：文化编辑界面的大图标
        /// 原理：使用 Transpiler 查找所有读取 ideo.Color 的地方，将其替换为我们的 GetIconColor 方法。
        /// </summary>
        [HarmonyPatch(typeof(IdeoUIUtility), "DoNameAndSymbol")]
        public static class Patch_DoNameAndSymbol
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // 查找目标方法：Ideo.get_Color
                var get_Color_Method = AccessTools.PropertyGetter(typeof(Ideo), nameof(Ideo.Color));
                // 替换方法：Patch_IdeoIconColor.GetIconColor
                var replacement_Method = AccessTools.Method(typeof(Patch_IdeoIconColor), nameof(GetIconColor));

                foreach (var code in instructions)
                {
                    // 如果指令是调用 ideo.Color
                    if (code.Calls(get_Color_Method))
                    {
                        // 替换为调用我们的静态方法，它接受栈顶的 Ideo 实例并返回 Color
                        yield return new CodeInstruction(OpCodes.Call, replacement_Method);
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
        }

        /// <summary>
        /// 补丁 2: 拦截 IdeoUIUtility.DoIdeoIcon
        /// 作用于：游戏内顶部栏、社交栏等小图标
        /// 原理：使用 Prefix 直接接管绘制逻辑。因为这个方法比较短，重写逻辑比 Transpiler 更直观且不易出错。
        /// </summary>
        [HarmonyPatch(typeof(IdeoUIUtility), "DoIdeoIcon")]
        public static class Patch_DoIdeoIcon
        {
            [HarmonyPrefix]
            public static bool Prefix(Rect rect, Ideo ideo, bool doTooltip, Action extraAction)
            {
                // 如果不是渡鸦图标，执行原版逻辑
                if (ideo == null || ideo.iconDef != RavenIcon)
                {
                    return true;
                }

                // --- 执行自定义绘制逻辑 (参考原版 IdeoUIUtility.DoIdeoIcon) ---

                // 1. 处理鼠标悬停高亮和提示
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                    if (doTooltip)
                    {
                        TooltipHandler.TipRegion(rect, ideo.name);
                    }
                }

                // 2. 绘制图标 (强制使用白色，即原色)
                GUI.color = Color.white;
                // 原版调用的是 ideo.DrawIcon(rect)，它内部会再次染色。
                // 所以我们直接画贴图，绕过 ideo.DrawIcon
                if (ideo.Icon != null)
                {
                    GUI.DrawTexture(rect, ideo.Icon);
                }
                GUI.color = Color.white; // 恢复颜色

                // 3. 处理点击事件
                if (extraAction != null && Widgets.ButtonInvisible(rect, true))
                {
                    extraAction();
                }

                // 返回 false 阻止原版方法执行
                return false;
            }
        }

        /// <summary>
        /// 辅助方法：根据图标类型决定返回文化颜色还是白色
        /// </summary>
        public static Color GetIconColor(Ideo ideo)
        {
            if (ideo != null && ideo.iconDef == RavenIcon)
            {
                return Color.white; // 渡鸦图标保持原色
            }
            return ideo?.Color ?? Color.white; // 其他图标使用文化颜色
        }
    }
}