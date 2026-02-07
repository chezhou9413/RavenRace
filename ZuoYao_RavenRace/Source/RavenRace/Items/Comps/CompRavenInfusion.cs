using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Items.Comps
{
    public class CompProperties_RavenInfusion : CompProperties
    {
        public CompProperties_RavenInfusion() => this.compClass = typeof(CompRavenInfusion);
    }

    public class CompRavenInfusion : ThingComp
    {
        private int poisonCharges = 0;

        public int PoisonCharges
        {
            get => poisonCharges;
            set => poisonCharges = Mathf.Max(0, value);
        }

        public bool IsPoisoned => poisonCharges > 0;

        public void AddCharges(int amount)
        {
            poisonCharges += amount;
        }

        public bool ConsumeCharge()
        {
            if (poisonCharges > 0)
            {
                poisonCharges--;
                return true;
            }
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref poisonCharges, "ravenPoisonCharges", 0);
        }

        // 修改武器名称 (类似 Infusion)
        public override string TransformLabel(string label)
        {
            if (IsPoisoned)
            {
                return $"{label} (淬毒)";
            }
            return label;
        }

        // 在检查面板显示信息
        public override string CompInspectStringExtra()
        {
            if (IsPoisoned)
            {
                return $"<color=#FF69B4>催情毒素: {poisonCharges} 次</color>";
            }
            return null;
        }

        public override void PostSplitOff(Thing other)
        {
            base.PostSplitOff(other);
            if (other is ThingWithComps twc)
            {
                var otherComp = twc.GetComp<CompRavenInfusion>();
                if (otherComp != null)
                {
                    otherComp.poisonCharges = this.poisonCharges;
                }
            }
        }
    }
}