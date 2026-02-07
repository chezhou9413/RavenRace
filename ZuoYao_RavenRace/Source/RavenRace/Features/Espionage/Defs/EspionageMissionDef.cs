using System;
using Verse;
using RimWorld;
using RavenRace.Features.Espionage.Workers;

namespace RavenRace.Features.Espionage
{
    public enum MissionType
    {
        GatherIntel,
        StealSupplies,
        Sabotage,
        Bribe,
        Turncoat,
        Assassinate
    }

    public class EspionageMissionDef : Def
    {
        // [修复] 使用 new 关键字隐藏基类 Def 的 description 字段
        // 或者直接移除它，因为 Def 已经有 description 了，除非你想覆盖它的加载逻辑。
        // 为了消除警告且保持原有逻辑，我们使用 new。
        public new string description;

        public int difficultyLevel = 1;
        public float baseDurationDays = 3f;
        public float baseSuccessChance = 0.6f;

        public int costIntel = 0;
        public int costMoney = 0;
        public int costInfluence = 0;

        public bool requiresSpy = true;
        public bool requiresTargetOfficial = false;

        public float rewardIntel = 0f;
        public ThingDef rewardItem;
        public int rewardItemCount = 0;

        public MissionType missionType = MissionType.GatherIntel;

        public Type workerClass = typeof(EspionageMissionWorker);

        [Unsaved]
        private EspionageMissionWorker workerInt;
        public EspionageMissionWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    if (workerClass == null) workerClass = typeof(EspionageMissionWorker);

                    workerInt = (EspionageMissionWorker)Activator.CreateInstance(workerClass);
                    workerInt.def = this;
                }
                return workerInt;
            }
        }
    }
}