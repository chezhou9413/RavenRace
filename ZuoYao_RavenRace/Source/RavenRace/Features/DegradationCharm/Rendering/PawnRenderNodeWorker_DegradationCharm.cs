using Verse;
using RimWorld;
using UnityEngine;

namespace RavenRace.Features.DegradationCharm.Rendering
{
    public class PawnRenderNodeWorker_DegradationCharm : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (node.tree.pawn.Dead)
            {
                return false;
            }
            if (parms.pawn.GetPosture().InBed())
            {
                return parms.facing == Rot4.South;
            }
            if (parms.Portrait || parms.pawn.Downed)
            {
                return true;
            }
            return parms.pawn.GetPosture() == PawnPosture.Standing;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            if (parms.Portrait)
            {
                result.z += 0.05f;
            }
            return result;
        }
    }
}