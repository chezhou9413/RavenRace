using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class Verb_BinahShockwave : Verb_CastAbility
    {
        // 贴图路径
        private static readonly Material ShockwaveChargeMat = MaterialPool.MatFrom("Races/Raven/Special/Binah/Abilities/Shockwave", ShaderDatabase.MoteGlow);

        // 由 Patch 调用
        public void DrawWarmupEffect(Stance_Warmup warmup)
        {
            if (CasterPawn == null) return;

            Vector3 center = CasterPawn.DrawPos;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 旋转效果
            float angle = (Time.realtimeSinceStartup * 200f) % 360f;
            // 脉动效果
            float pulse = 1f + Mathf.Sin(Time.realtimeSinceStartup * 10f) * 0.1f;

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(8f * pulse, 1f, 8f * pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ShockwaveChargeMat, 0);
        }
    }
}