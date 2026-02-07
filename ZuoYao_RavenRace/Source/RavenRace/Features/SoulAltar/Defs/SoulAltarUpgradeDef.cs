using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RavenRace
{
    public enum AltarComponentType
    {
        Infuser,  // 内环
        Pylon,    // 外环
        Injector  // 强化
    }

    public class SoulAltarUpgradeDef : Def
    {
        public ThingDef inputItem;
        public AltarComponentType slotType = AltarComponentType.Infuser;

        // 效果定义
        public List<StatModifier> statOffsets;
        public List<TraitDef> forcedTraits;
        public List<SkillGain> skillGains;
        public List<HediffDef> hediffs;

        public class SkillGain
        {
            public SkillDef skill;
            public int xp;
            public Passion? passion;
        }
    }
}