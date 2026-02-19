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
            // --- [BUG FIX] ---
            // 核心修复：处理代孕任务中 carrier 和 partner 均为 null 的情况。
            // 在这种情况下，父母是匿名的扶桑成员，我们直接设置名称为“扶桑”，并返回。
            // 这样可以防止后续代码因访问 null Pawn 的属性（如 carrier.gender）而引发 NullReferenceException。
            if (carrier == null && partner == null)
            {
                this.motherName = "扶桑"; // "Fusang"
                this.fatherName = "扶桑"; // "Fusang"
                this.faction = Faction.OfPlayer; // 保证派系正确
                // motherId 和 fatherId 保持 null
                return;
            }

            // --- 健壮性增强 ---
            // 如果只有一个为 null，则将另一个作为双亲，确保 carrier 永远不为 null。
            if (carrier == null) carrier = partner;
            if (partner == null) partner = carrier;
            // -----------------

            Pawn bioFather = null;
            Pawn bioMother = null;

            bool sameSex = (carrier.gender == partner.gender);
            bool mechInvolved = carrier.RaceProps.IsMechanoid || partner.RaceProps.IsMechanoid;

            if (sameSex || mechInvolved)
            {
                // 在特殊情况下，carrier 强制为 Mother (生物学载体)，partner 强制为 Father (供体)
                bioMother = carrier;
                bioFather = partner;
            }
            else
            {
                // 正常异性逻辑
                if (carrier.gender == Gender.Female) bioMother = carrier; else bioFather = carrier;
                if (partner.gender == Gender.Male) bioFather = partner; else bioMother = partner;
            }

            if (bioMother == null) bioMother = carrier;
            if (bioFather == null) bioFather = partner;

            this.motherId = bioMother?.ThingID;
            this.fatherId = bioFather?.ThingID;
            this.motherName = bioMother?.LabelShort ?? "Unknown";
            this.fatherName = bioFather?.LabelShort ?? "Unknown";
            this.faction = Faction.OfPlayer;
        }

        private void DetermineRace(Pawn carrier, Pawn partner)
        {
            // 如果因为修复导致 carrier 和 partner 都为 null，则默认后代为纯血渡鸦
            if (carrier == null && partner == null)
            {
                this.pawnKind = RavenDefOf.Raven_Colonist;
                return;
            }

            // 保证 carrier 不为 null
            if (carrier == null) carrier = partner;

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
                    else if (isCarrierRaven) targetRaceDef = partner?.def ?? carrier.def; // 安全检查
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
            // 这里的 parent1 和 parent2 可能是 null
            var p1Data = GetOrSimulateBloodline(parent1);
            var p2Data = GetOrSimulateBloodline(parent2);

            this.goldenCrowConcentration = Mathf.Max(0.01f, (p1Data.concentration + p2Data.concentration) / 2f + Rand.Range(-0.02f, 0.02f));

            Dictionary<string, float> tempComp = new Dictionary<string, float>();
            HashSet<string> allRaces = new HashSet<string>();

            if (p1Data.composition != null)
                foreach (var k in p1Data.composition.Keys) if (k != "Human") allRaces.Add(k);
            if (p2Data.composition != null)
                foreach (var k in p2Data.composition.Keys) if (k != "Human") allRaces.Add(k);

            // 纯血代孕蛋强制包含渡鸦血脉
            allRaces.Add("Raven_Race");

            foreach (var race in allRaces)
            {
                float val1 = p1Data.composition != null && p1Data.composition.ContainsKey(race) ? p1Data.composition[race] : 0f;
                float val2 = p2Data.composition != null && p2Data.composition.ContainsKey(race) ? p2Data.composition[race] : 0f;
                float baseVal = (val1 + val2) / 2f;
                if (baseVal > 0) tempComp[race] = baseVal;
            }

            // 如果是代孕蛋，tempComp可能是空的，需要手动添加渡鸦血脉
            if (!tempComp.Any())
            {
                tempComp["Raven_Race"] = 1.0f;
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
                float scaleFactor = (1f - currentRaven) > 0 ? 0.5f / (1f - currentRaven) : 0f;
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