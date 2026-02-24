using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RavenRace.Features.MiscSmallFeatures.Bathtub
{
    public class Building_RavenBathtub : Building
    {
        private Graphic liquidGraphicCache;

        private Graphic LiquidGraphic
        {
            get
            {
                if (liquidGraphicCache == null)
                {
                    var ext = def.GetModExtension<DefModExtension_BathtubGraphics>();
                    if (ext?.liquidGraphic != null)
                    {
                        liquidGraphicCache = ext.liquidGraphic.GraphicColoredFor(this);
                    }
                }
                return liquidGraphicCache;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (LiquidGraphic != null)
            {
                Vector3 liquidPos = drawLoc;
                // 覆盖在躺下的小人上方
                liquidPos.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.05f;
                LiquidGraphic.Draw(liquidPos, Rotation, this, 0f);
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var gizmo in base.GetFloatMenuOptions(selPawn)) yield return gizmo;

            if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("无法到达浴缸", null);
                yield break;
            }

            IntVec3 targetCell = IntVec3.Invalid;
            foreach (IntVec3 cell in this.OccupiedRect())
            {
                bool occupied = false;
                List<Thing> things = cell.GetThingList(this.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn p && p.CurJobDef == RavenDefOf.Raven_Job_TakeSlimeBath)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    targetCell = cell;
                    break;
                }
            }

            if (!targetCell.IsValid)
            {
                yield return new FloatMenuOption("浴缸已满", null);
            }
            else
            {
                yield return new FloatMenuOption("泡黏液浴", delegate ()
                {
                    Job job = JobMaker.MakeJob(RavenDefOf.Raven_Job_TakeSlimeBath, this, targetCell);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }
}