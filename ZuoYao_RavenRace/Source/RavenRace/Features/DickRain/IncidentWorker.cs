using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.DickRain
{
    public class IncidentWorker_DickRain : IncidentWorker
    {
        // 获取我们自定义天气的 Def
        private static WeatherDef DickRainWeather => DefDatabase<WeatherDef>.GetNamed("DickRain_Weather");

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.weatherManager.curWeather != DickRainWeather;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            map.weatherManager.TransitionTo(DickRainWeather);
            SendStandardLetter("迪克雨来袭", "老天爷，你下屌吧，操死我吧。 ——余华",LetterDefOf.ThreatSmall, parms,LookTargets.Invalid);
            return true;
        }
    }
}
