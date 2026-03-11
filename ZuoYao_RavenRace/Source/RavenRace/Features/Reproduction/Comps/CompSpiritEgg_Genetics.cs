using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RavenRace.Compat.Epona;
using RavenRace.Features.Bloodline;
using RavenRace.Features.RavenRite.Rite_Promotion.Purification.Comps; // [新增引用] 引入纯化系统命名空间

namespace RavenRace.Features.Reproduction
{
    public partial class CompSpiritEgg
    {
        // [核心修改] 增加 customBloodline 参数，接收来自母体怀孕状态里特殊的跨物种属性
        public void Initialize(Pawn carrier, Pawn partner, GeneSet inheritedGenes, string customBloodline = null)
        {
            DetermineParents(carrier, partner, customBloodline);
            DetermineRace(carrier, partner);

            this.geneSet = inheritedGenes;
            if (carrier != null && partner != null && GeneUtility.SameHeritableXenotype(carrier, partner))
            {
                this.xenotypeName = carrier.genes?.xenotypeName;
                this.iconDef = carrier.genes?.iconDef;
            }

            CalculateBloodlineInheritance(carrier, partner, customBloodline);
        }

        private void DetermineParents(Pawn carrier, Pawn partner, string customBloodline)
        {
            // 如果父母都是 null 且没有任何特殊血脉指定，这就是匿名扶桑的代孕蛋
            if (carrier == null && partner == null && string.IsNullOrEmpty(customBloodline))
            {
                this.motherName = "扶桑";
                this.fatherName = "扶桑";
                this.faction = Faction.OfPlayer;
                return;
            }

            if (carrier == null) carrier = partner;
            if (partner == null) partner = carrier;

            Pawn bioFather = null;
            Pawn bioMother = null;

            // 特殊彩蛋：与墙体发生关系
            if (customBloodline == "Wall" && partner == carrier)
            {
                // 自己作为母体，父亲是抽象的墙
                bioMother = carrier;
                bioFather = null;
            }
            else
            {
                bool sameSex = (carrier.gender == partner.gender);
                bool mechInvolved = carrier.RaceProps.IsMechanoid || partner.RaceProps.IsMechanoid;

                if (sameSex || mechInvolved)
                {
                    bioMother = carrier;
                    bioFather = partner;
                }
                else
                {
                    if (carrier.gender == Gender.Female) bioMother = carrier; else bioFather = carrier;
                    if (partner.gender == Gender.Male) bioFather = partner; else bioMother = partner;
                }
            }

            if (bioMother == null) bioMother = carrier;

            this.motherId = bioMother?.ThingID;
            this.fatherId = bioFather?.ThingID;
            this.motherName = bioMother?.LabelShort ?? "Unknown";

            // 如果是墙体导致的无生物学父亲，强制给一个拉风的名字
            if (customBloodline == "Wall" && bioFather == null)
            {
                this.fatherName = "某面厚重坚实的墙体";
            }
            else
            {
                this.fatherName = bioFather?.LabelShort ?? "Unknown";
            }

            this.faction = Faction.OfPlayer;
        }

        private void DetermineRace(Pawn carrier, Pawn partner)
        {
            if (carrier == null && partner == null)
            {
                this.pawnKind = RavenDefOf.Raven_Colonist;
                return;
            }

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
                    else if (isCarrierRaven) targetRaceDef = partner?.def ?? carrier.def;
                    else targetRaceDef = carrier.def;
                }
            }
            else
            {
                targetRaceDef = carrier.def;
            }

            this.pawnKind = FindBasicPawnKindForRace(targetRaceDef);
        }

        // [核心修改] 处理外部强行塞入的自定义血脉（如 "Wall"）
        private void CalculateBloodlineInheritance(Pawn parent1, Pawn parent2, string customBloodline)
        {
            var p1Data = GetOrSimulateBloodline(parent1);
            var p2Data = GetOrSimulateBloodline(parent2);

            // 如果触发了和墙体的彩蛋，强行将另一方的血脉数据覆盖为 100% 的墙之血脉
            if (!string.IsNullOrEmpty(customBloodline))
            {
                p2Data = (0f, new Dictionary<string, float> { { customBloodline, 1.0f } });
            }

            this.goldenCrowConcentration = Mathf.Max(0.01f, (p1Data.concentration + p2Data.concentration) / 2f + Rand.Range(-0.02f, 0.02f));

            Dictionary<string, float> tempComp = new Dictionary<string, float>();
            HashSet<string> allRaces = new HashSet<string>();

            if (p1Data.composition != null)
                foreach (var k in p1Data.composition.Keys) if (k != "Human") allRaces.Add(k);
            if (p2Data.composition != null)
                foreach (var k in p2Data.composition.Keys) if (k != "Human") allRaces.Add(k);

            allRaces.Add("Raven_Race");

            foreach (var race in allRaces)
            {
                float val1 = p1Data.composition != null && p1Data.composition.ContainsKey(race) ? p1Data.composition[race] : 0f;
                float val2 = p2Data.composition != null && p2Data.composition.ContainsKey(race) ? p2Data.composition[race] : 0f;
                float baseVal = (val1 + val2) / 2f;
                if (baseVal > 0) tempComp[race] = baseVal;
            }

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

        /// <summary>
        /// 获取或模拟指定父母的血脉数据。
        /// [核心修改] 适应解耦架构，从独立的 CompPurification 中读取金乌浓度。
        /// </summary>
        private (float concentration, Dictionary<string, float> composition) GetOrSimulateBloodline(Pawn p)
        {
            if (p == null) return (0f, new Dictionary<string, float>());

            // 机械体返回纯净的机械血脉
            if (p.RaceProps.IsMechanoid)
            {
                return (0f, new Dictionary<string, float> { { BloodlineManager.MECHANIOD_BLOODLINE_KEY, 1.0f } });
            }

            string raceKey = p.def.defName;
            if (EponaCompatUtility.IsEponaActive)
            {
                raceKey = EponaCompatUtility.NormalizeToEponaKey(raceKey);
            }

            // 雪牛恶作剧兼容
            if (RavenRaceMod.Settings != null && RavenRaceMod.Settings.enableMuffaloPrank && p.def.defName == "Muffalo")
                return (0f, new Dictionary<string, float> { { "MooGirl", 1.0f } });

            // 获取杂交血脉组件
            CompBloodline comp = p.TryGetComp<CompBloodline>();
            // [新增] 获取纯化（金乌）组件
            CompPurification purComp = p.TryGetComp<CompPurification>();

            // 从纯化组件安全读取金乌浓度
            float conc = purComp != null ? purComp.GoldenCrowConcentration : 0f;

            if (comp != null && comp.BloodlineComposition.Count > 0)
            {
                // 返回金乌浓度和杂交组成
                return (conc, comp.BloodlineComposition);
            }

            // 如果是非渡鸦的外星人，模拟一个100%自身种族的字典，浓度为0
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