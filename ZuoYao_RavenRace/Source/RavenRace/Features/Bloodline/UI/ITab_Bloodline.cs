using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Features.Bloodline; // [Added]

namespace RavenRace
{
    public class ITab_Bloodline : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(300f, 400f);

        // [Change] Comp_Bloodline -> CompBloodline
        private CompBloodline cachedComp;

        public ITab_Bloodline()
        {
            this.size = WinSize;
            this.labelKey = "RavenRace_Bloodline";
        }

        private CompBloodline Comp
        {
            get
            {
                Pawn pawn = base.SelPawn;
                if (pawn == null) return null;
                if (cachedComp != null && cachedComp.parent == pawn) return cachedComp;
                cachedComp = pawn.TryGetComp<CompBloodline>();
                return cachedComp;
            }
        }

        public override bool IsVisible => Comp != null;

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Rect rectTop = rect;
            rectTop.height = 30f;

            Text.Font = GameFont.Medium;
            Widgets.Label(rectTop, "RavenRace_Bloodline".Translate());
            Text.Font = GameFont.Small;

            Rect rectContent = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rectContent);

            CompBloodline comp = this.Comp;
            if (comp != null)
            {
                DrawConcentrationBar(listing, comp.GoldenCrowConcentration);

                listing.Gap(12f);
                listing.GapLine();
                listing.Gap(12f);

                listing.Label("RavenRace_BloodlineComposition".Translate() + ":");
                listing.Gap(4f);

                if (comp.BloodlineComposition != null)
                {
                    if (comp.BloodlineComposition.ContainsKey("Raven_Race"))
                    {
                        DrawCompositionRow(listing, "RavenRace_RaceName".Translate(), comp.BloodlineComposition["Raven_Race"]);
                    }

                    foreach (var entry in comp.BloodlineComposition)
                    {
                        if (entry.Key == "Raven_Race") continue;

                        string displayLabel = GetBloodlineDisplayLabel(entry.Key);
                        DrawCompositionRow(listing, displayLabel, entry.Value);
                    }
                }
            }
            else
            {
                listing.Label("Error: No bloodline component found.");
            }

            listing.End();
        }

        private string GetBloodlineDisplayLabel(string raceDefName)
        {
            ThingDef raceDef = DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);
            if (raceDef != null) return raceDef.LabelCap;
            return raceDefName;
        }

        private void DrawConcentrationBar(Listing_Standard listing, float concentration)
        {
            listing.Label("RavenRace_GoldenCrowConcentration".Translate() + ": " + concentration.ToString("P1"));
            Rect barRect = listing.GetRect(24f);
            Widgets.DrawBoxSolid(barRect, new Color(0.2f, 0.2f, 0.2f));
            Rect fillRect = barRect;
            fillRect.width *= concentration;
            Widgets.DrawBoxSolid(fillRect, new Color(1f, 0.8f, 0.2f));
            Widgets.DrawBox(barRect);
            TooltipHandler.TipRegion(barRect, "RavenRace_BloodlinePurity".Translate() + ": " + concentration.ToString("P2"));
        }

        private void DrawCompositionRow(Listing_Standard listing, string label, float percent)
        {
            Rect rect = listing.GetRect(24f);
            Rect left = rect.LeftPart(0.7f);
            Widgets.Label(left, label);
            Rect right = rect.RightPart(0.3f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(right, percent.ToString("P1"));
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}