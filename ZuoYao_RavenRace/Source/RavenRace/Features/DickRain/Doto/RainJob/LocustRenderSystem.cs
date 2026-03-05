using RavenRace.Features.DickRain.Doto.Data;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace RavenRace.Features.DickRain.Doto.RainJob
{
    public static class LocustRenderSystem
    {
        private static readonly Matrix4x4[] s_batchBody = new Matrix4x4[1023];

        public static void Draw(NativeArray<LocustData> locusts, Material mat, int drawCount)
        {
            if (mat == null) return;

            int batchCount = 0;
            int limit = Mathf.Min(drawCount, locusts.Length);

            for (int i = 0; i < limit; i++)
            {
                LocustData l = locusts[i];
                s_batchBody[batchCount++] = Matrix4x4.TRS(
                    new Vector3(l.position.x, 15f, l.position.y),
                    Quaternion.AngleAxis(l.angle, Vector3.up),
                    new Vector3(7f, 1f, 7f)
                );

                if (batchCount == 1023)
                {
                    Flush(mat, batchCount);
                    batchCount = 0;
                }
            }

            if (batchCount > 0)
                Flush(mat, batchCount);
        }

        private static void Flush(Material mat, int count)
        {
            Graphics.DrawMeshInstanced(
                MeshPool.plane10, 0, mat, s_batchBody, count,
                null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
        }
    }
}