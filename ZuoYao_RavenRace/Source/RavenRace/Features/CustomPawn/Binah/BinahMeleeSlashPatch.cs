using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    /// <summary>
    /// 为 Binah 的近战攻击添加月牙斩击特效的 Harmony 补丁。
    /// 命中时向 BinahSlashEffectManager 注册一个斩击特效请求，
    /// 由 Manager 在渲染帧中用 Graphics.DrawMesh 以任意角度绘制。
    /// 注意：TryCastShot 是 protected 方法，必须用字符串指定方法名。
    /// </summary>
    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    public static class Patch_Verb_MeleeAttack_TryCastShot_BinahSlash
    {
        /// <summary>
        /// Postfix 补丁：命中（__result=true）时触发斩击特效注册。
        /// </summary>
        public static void Postfix(Verb_MeleeAttack __instance, bool __result)
        {
            // 只在命中时生成特效
            if (!__result) return;

            Pawn caster = __instance.CasterPawn;
            if (caster == null || !caster.Spawned || caster.Map == null) return;

            // 仅对 Binah 生效
            if (caster.kindDef != BinahDefOf.Raven_PawnKind_Binah) return;

            Thing targetThing = __instance.CurrentTarget.Thing;
            if (targetThing == null) return;

            // 获取或创建当前地图上的斩击特效管理器
            BinahSlashEffectManager manager = caster.Map.GetComponent<BinahSlashEffectManager>();
            if (manager == null)
            {
                manager = new BinahSlashEffectManager(caster.Map);
                caster.Map.components.Add(manager);
            }

            // 计算攻击方向：施法者 → 目标
            Vector3 attackerPos = caster.DrawPos;
            Vector3 targetPos = targetThing.DrawPos;
            Vector3 direction = targetPos - attackerPos;

            // 斩击位置：在连线上按比例偏移，偏向目标侧
            // SlashPositionLerp：0=攻击者位置，1=目标位置，0.65=偏向目标
            Vector3 slashPos = Vector3.Lerp(attackerPos, targetPos, BinahSlashEffectManager.SlashPositionLerp);
            slashPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 计算旋转角度：
            // Atan2(-z, x) 以"正东(右)"为0°，与 Graphics.DrawMesh 的坐标系匹配。
            // 再加上 TextureDirectionCorrection 补偿贴图本身的朝向偏差：
            //   贴图朝左(西) → 填 180f
            //   贴图朝右(东) → 填 0f
            //   贴图朝上(北) → 填 -90f
            //   贴图朝下(南) → 填 90f
            float baseAngle = Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + BinahSlashEffectManager.TextureDirectionCorrection;

            // 向 Manager 注册一个新的斩击特效
            manager.Register(new BinahSlashEffect(slashPos, finalAngle));
        }
    }
}