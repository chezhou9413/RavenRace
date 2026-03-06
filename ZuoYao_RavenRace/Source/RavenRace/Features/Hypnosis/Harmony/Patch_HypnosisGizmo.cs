using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.Hypnosis.Harmony
{
    /// <summary>
    /// 催眠系统专用的 Gizmo 补丁。
    /// 独立于侍奉系统，确保模块解耦。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_HypnosisGizmo
    {
        // 缓存图标，避免每帧加载
        private static Texture2D IconOpenApp;

        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            // 1. 先返回前面的 Gizmo，确保不覆盖原版和其他 Mod 的按钮
            foreach (var gizmo in __result)
            {
                yield return gizmo;
            }

            // 2. 检查是否为玩家控制的殖民者且存活
            if (__instance.IsColonistPlayerControlled && !__instance.Dead)
            {
                // 3. 检查是否是任何人的催眠主人 (Master)
                // 访问 WorldComponent 检查是否有绑定记录
                if (WorldComponent_Hypnosis.Instance.IsMaster(__instance))
                {
                    if (IconOpenApp == null)
                    {
                        // 确保你有这个贴图，如果没有会显示粉色方块或红叉
                        IconOpenApp = ContentFinder<Texture2D>.Get("UI/Commands/OpenHypnosisApp", true);
                    }

                    yield return new Command_Action
                    {
                        defaultLabel = "打开催眠App",
                        defaultDesc = "启动“深红迷梦”终端控制界面，向已建立链接的受控体发送指令。",
                        icon = IconOpenApp ?? BaseContent.BadTex,
                        action = () =>
                        {
                            // 打开控制窗口
                            Find.WindowStack.Add(new Dialog_HypnosisControl(__instance));
                        }
                    };
                }
            }
        }
    }
}