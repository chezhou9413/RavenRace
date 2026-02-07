using System.Collections.Generic;
using Verse;

namespace RavenRace.Features.Operator.Gifting
{
    public class GiftReactionDef : Def
    {
        public bool isDefault = false;
        public List<ThingDef> thingDefs;
        public List<ThingCategoryDef> thingCategoryDefs;
        public float favorChangePerValue = 0f;
        public int maxFavorChange = 9999;
        public string specialMessage;
    }
}