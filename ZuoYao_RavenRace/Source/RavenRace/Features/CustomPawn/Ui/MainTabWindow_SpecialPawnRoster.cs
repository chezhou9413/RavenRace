using RavenRace.Features.CustomPawn.Ui.UiWindows;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui
{
    public class MainTabWindow_SpecialPawnRoster : MainTabWindow
    {
        public override Vector2 InitialSize
        {
            get { return Vector2.zero; }
        }

        public override void DoWindowContents(Rect inRect) { }

        public override void PreOpen()
        {
            base.PreOpen();
            Find.WindowStack.Add(new Dialog_SpecialPawnRoster());
            Find.MainTabsRoot.EscapeCurrentTab(false);
        }
    }
}
