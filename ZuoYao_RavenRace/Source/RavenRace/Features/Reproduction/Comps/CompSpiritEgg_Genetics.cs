using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Compat.Epona;
using RavenRace.Features.Bloodline;

// [关键] 命名空间统一
namespace RavenRace.Features.Reproduction
{
    public partial class CompSpiritEgg
    {
        public void Initialize(Pawn carrier, Pawn partner, GeneSet inheritedGenes)
        {
            DetermineParents(carrier, partner);
            DetermineRace(carrier, partner);

            this.geneSet = inheritedGenes;
            if (carrier != null && partner != null && GeneUtility.SameHeritableXenotype(carrier, partner))
            {
                this.xenotypeName = carrier.genes?.xenotypeName;
                this.iconDef = carrier.genes?.iconDef;
            }

            CalculateBloodlineInheritance(carrier, partner);
        }

        private void DetermineParents(Pawn carrier, Pawn partner)
        {
            // [新增逻辑] 兼容同性/机械/男性生蛋
            // 原逻辑可能会在性别相同时混淆 Father/Mother

            Pawn bioFather = null;
            Pawn bioMother = null;

            bool sameSex = (partner != null && carrier.gender == partner.gender);
            bool mechInvolved = carrier.RaceProps.IsMechanoid || (partner != null && partner.RaceProps.IsMechanoid);

            if (sameSex || mechInvolved)
            {
                // 在特殊情况下，carrier 强制为 Mother (生物学载体)，partner 强制为 Father (供体)
                bioMother = carrier;
                bioFather = partner ?? carrier;
            }
            else
            {
                // 正常异性逻辑
                if (carrier.gender == Gender.Female) bioMother = carrier; else bioFather = carrier;
                if (partner != null)
                {
                    if (partner.gender == Gender.Male) bioFather = partner; else bioMother = partner;
                }
            }

            if (bioMother == null) bioMother = carrier;
            if (bioFather == null) bioFather = partner ?? carrier;

            this.motherId = bioMother?.ThingID;
            this.fatherId = bioFather?.ThingID;
            this.motherName = bioMother?.LabelShort ?? "Unknown";
            this.fatherName = bioFather?.LabelShort ?? "Unknown";
            this.faction = Faction.OfPlayer;
        }

        private void DetermineRace(Pawn carrier, Pawn partner)
        {
            bool forceRaven = RavenRaceMod.Settings != null && RavenRaceMod.Settings.forceRavenDescendant;
            bool isCarrierRaven = carrier.def == RavenDefOf.Raven_Race;
            bool isPartnerRaven = (partner != null && partner.def == RavenDefOf.Raven_Race);
            bool anyRaven = isCarrierRaven || isPartnerRaven;

            ThingDef targetRaceDef;
            if (forceRaven && anyRaven)
            {
                targetRaceDef = RavenDefOf.Raven_Race;
            }
            else if (anyRaven)
            {
                if (Rand.Bool)
                    targetRaceDef = RavenDefOf.Raven_Race;
                else
                {
                    if (isCarrierRaven && isPartnerRaven) targetRaceDef = carrier.def;
                    else if (isCarrierRaven) targetRaceDef = partner.def;
                    else targetRaceDef = carrier.def;
                }
            }
            else
            {
                targetRaceDef = carrier.def;
            }

            this.pawnKind = FindBasicPawnKindForRace(targetRaceDef);
        }

        private void CalculateBloodlineInheritance(Pawn parent1, Pawn parent2)
        {
            // 这里的 parent1 和 parent2 可能是机械
            var p1Data = GetOrSimulateBloodline(parent1);
            var p2Data = GetOrSimulateBloodline(parent2);

            this.goldenCrowConcentration = Mathf.Max(0.01f, (p1Data.concentration + p2Data.concentration) / 2f + Rand.Range(-0.02f, 0.02f));

            Dictionary<string, float> tempComp = new Dictionary<string, float>();
            HashSet<string> allRaces = new HashSet<string>();
            foreach (var k in p1Data.composition.Keys) if (k != "Human") allRaces.Add(k);
            foreach (var k in p2Data.composition.Keys) if (k != "Human") allRaces.Add(k);
            allRaces.Add("Raven_Race");

            foreach (var race in allRaces)
            {
                float val1 = p1Data.composition.ContainsKey(race) ? p1Data.composition[race] : 0f;
                float val2 = p2Data.composition.ContainsKey(race) ? p2Data.composition[race] : 0f;
                float baseVal = (val1 + val2) / 2f;
                if (baseVal > 0) tempComp[race] = baseVal;
            }

            float currentRaven = tempComp.ContainsKey("Raven_Race") ? tempComp["Raven_Race"] : 0f;
            float totalOther = 0f;
            foreach (var kvp in tempComp) if (kvp.Key != "Raven_Race") totalOther += kvp.Value;
            float currentTotal = currentRaven + totalOther;

            if (currentTotal <= 0.001f) { this.bloodlineComposition["Raven_Race"] = 1.0f; return; }

            currentRaven /= currentTotal;
            Dictionary<string, float> normalizedOthers = new Dictionary<string, float>();
            foreach (var kvp in tempComp) if (kvp.Key != "Raven_Race") normalizedOthers[kvp.Key] = kvp.Value / currentTotal;

            if (currentRaven < 0.5f)
            {
                this.bloodlineComposition["Raven_Race"] = 0.5f;
                float scaleFactor = 0.5f / (1.0f - currentRaven);
                foreach (var kvp in normalizedOthers) this.bloodlineComposition[kvp.Key] = kvp.Value * scaleFactor;
            }
            else
            {
                this.bloodlineComposition["Raven_Race"] = currentRaven;
                foreach (var kvp in normalizedOthers) this.bloodlineComposition[kvp.Key] = kvp.Value;
            }

            BloodlineUtility.EnsureBloodlineFloor(this.bloodlineComposition);
        }

        private (float concentration, Dictionary<string, float> composition) GetOrSimulateBloodline(Pawn p)
        {
            if (p == null) return (0f, new Dictionary<string, float>());

            // [新增] 机械族判断
            if (p.RaceProps.IsMechanoid)
            {
                return (0f, new Dictionary<string, float> { { BloodlineManager.MECHANIOD_BLOODLINE_KEY, 1.0f } });
            }



            string raceKey = p.def.defName;
            if (EponaCompatUtility.IsEponaActive)
            {
                raceKey = EponaCompatUtility.NormalizeToEponaKey(raceKey);
            }

            if (RavenRaceMod.Settings != null && RavenRaceMod.Settings.enableMuffaloPrank && p.def.defName == "Muffalo")
                return (0f, new Dictionary<string, float> { { "MooGirl", 1.0f } });

            CompBloodline comp = p.TryGetComp<CompBloodline>();
            if (comp != null && comp.BloodlineComposition.Count > 0)
            {
                return (comp.GoldenCrowConcentration, comp.BloodlineComposition);
            }

            return (0f, new Dictionary<string, float> { { raceKey, 1.0f } });
        }

        private PawnKindDef FindBasicPawnKindForRace(ThingDef raceDef)
        {
            if (raceDef == null) return PawnKindDefOf.Colonist;
            if (raceDef == RavenDefOf.Raven_Race) return RavenDefOf.Raven_Colonist;
            return PawnKindDefOf.Colonist;
        }
    }
}