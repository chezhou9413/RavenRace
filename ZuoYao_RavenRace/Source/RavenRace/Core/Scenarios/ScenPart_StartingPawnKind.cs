using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;

namespace RavenRace
{
    // 自定义的剧本部分，用于强制指定开局 Pawn 的类型 (PawnKind)
    public class ScenPart_StartingPawnKind : ScenPart
    {
        public PawnKindDef pawnKind;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight);
            if (Widgets.ButtonText(scenPartRect, pawnKind?.LabelCap ?? "Select PawnKind..."))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefs)
                {
                    if (kind.RaceProps.Humanlike)
                    {
                        list.Add(new FloatMenuOption(kind.LabelCap, delegate
                        {
                            pawnKind = kind;
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        public override string Summary(Scenario scen)
        {
            return "RavenRace_ScenPart_StartingPawnKind".Translate(pawnKind?.LabelCap ?? "None");
        }

        public override void Randomize()
        {
            // 不随机，保持设定
        }
    }
}