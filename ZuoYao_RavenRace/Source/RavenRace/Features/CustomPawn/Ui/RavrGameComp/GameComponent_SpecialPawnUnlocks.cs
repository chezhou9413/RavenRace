using RavenRace.Features.CustomPawn.Ui.RaveExtension;
using RavenRace.Features.CustomPawn.Ui.SpecialPawnWorker;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.RavrGameComp
{
    public class GameComponent_SpecialPawnUnlocks : GameComponent
    {
        //解锁状态
        private Dictionary<PawnKindDef, bool> unlockStates;

        //UI预览用的Pawn,每次加载重新生成
        private Dictionary<PawnKindDef, Pawn> previewPawns;

        //给玩家的唯一Pawn实例
        private Dictionary<PawnKindDef, Pawn> playerPawns;

        //Scribe临时列表
        private List<PawnKindDef> tmpUnlockKeys;
        private List<bool> tmpUnlockValues;
        private List<PawnKindDef> tmpPlayerPawnKeys;
        private List<Pawn> tmpPlayerPawnValues;

        //有RaveCustomPawnUiData的KindDef 缓存
        private List<PawnKindDef> cachedSpecialKinds;

        public GameComponent_SpecialPawnUnlocks(Game game)
        {
            unlockStates = new Dictionary<PawnKindDef, bool>();
            previewPawns = new Dictionary<PawnKindDef, Pawn>();
            playerPawns = new Dictionary<PawnKindDef, Pawn>();
            tmpUnlockKeys = new List<PawnKindDef>();
            tmpUnlockValues = new List<bool>();
            tmpPlayerPawnKeys = new List<PawnKindDef>();
            tmpPlayerPawnValues = new List<Pawn>();
        }

        public static GameComponent_SpecialPawnUnlocks Instance
        {
            get
            {
                if (Current.Game == null)
                    return null;
                return Current.Game.GetComponent<GameComponent_SpecialPawnUnlocks>();
            }
        }

        // ── 初始化 ───────────────────────────────────────────────

        public override void FinalizeInit()
        {
            //扫描所有带扩展的KindDef
            cachedSpecialKinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(d => d.GetModExtension<RaveCustomPawnUiData>() != null)
                .ToList();

            foreach (PawnKindDef kindDef in cachedSpecialKinds)
            {
                //补全解锁状态
                if (!unlockStates.ContainsKey(kindDef))
                    unlockStates[kindDef] = false;

                // 预览Pawn：每次加载重新生成（不需要保存，仅用于UI展示）
                previewPawns[kindDef] = GeneratePreviewPawn(kindDef);

                //玩家Pawn只在第一次生成，之后复用同一个
                if (!playerPawns.ContainsKey(kindDef) || playerPawns[kindDef] == null)
                    playerPawns[kindDef] = GeneratePlayerPawn(kindDef);
            }
        }

        //生成 Pawn 并立刻注册到World
        private Pawn GenerateAndRegisterPawn(PawnKindDef kindDef)
        {
            Faction faction = FactionUtility.DefaultFactionFrom(kindDef.defaultFactionDef);
            Pawn pawn = PawnGenerator.GeneratePawn(kindDef, faction);
            //必须注册到 WorldPawns
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            return pawn;
        }

        /// <summary>预览用Pawn仅UI展示。</summary>
        private Pawn GeneratePreviewPawn(PawnKindDef kindDef)
        {
            return GenerateAndRegisterPawn(kindDef);
        }

        /// <summary>交给玩家的正式Pawn。</summary>
        private Pawn GeneratePlayerPawn(PawnKindDef kindDef)
        {
            return GenerateAndRegisterPawn(kindDef);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            foreach (PawnKindDef kindDef in cachedSpecialKinds)
            {
                RaveCustomPawnUiData ext = kindDef.GetModExtension<RaveCustomPawnUiData>();
                if (ext == null)
                    continue;

                SpecialPawnWorkerBase worker = ext.Worker;
                if (worker == null)
                    continue;

                if (IsUnlocked(kindDef))
                    continue;

                if (worker.UnlockCondition())
                {
                    Unlock(kindDef);
                    worker.OnUnlocked();
                }
                else
                {
                    worker.OnLocked();
                }
            }
        }

        //获取解锁状态
        public bool IsUnlocked(PawnKindDef kindDef)
        {
            if (kindDef == null)
                return false;

            bool value;
            if (unlockStates.TryGetValue(kindDef, out value))
                return value;
            return false;
        }

        public void SetUnlocked(PawnKindDef kindDef, bool unlocked)
        {
            if (kindDef == null)
                return;
            unlockStates[kindDef] = unlocked;
        }

        public void Unlock(PawnKindDef kindDef)
        {
            SetUnlocked(kindDef, true);
        }

        public void Lock(PawnKindDef kindDef)
        {
            SetUnlocked(kindDef, false);
        }

        //获取预览用的pawn
        public Pawn GetPreviewPawn(PawnKindDef kindDef)
        {
            Pawn pawn;
            previewPawns.TryGetValue(kindDef, out pawn);
            return pawn;
        }

        // 把 playerPawn 生成到地图上交给玩家
        public Pawn GivePawnToPlayer(PawnKindDef kindDef, IntVec3 spawnPos, Map map,RaveCustomPawnUiData data)
        {
            if (kindDef == null || map == null)
                return null;
            if (!spawnPos.IsValid || !spawnPos.InBounds(map))
                return null;
            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn) || pawn == null)
                return null;
            //已在场上或已死亡，不重复处理
            if (pawn.Spawned || pawn.Dead)
                return null;
            // 已在 GenerateAndRegisterPawn 时注册，无需再次 PassToWorld
            GenSpawn.Spawn(pawn, spawnPos, map);
            pawn.SetFaction(Faction.OfPlayer);
            data.Worker.UiSummon(pawn);
            return pawn;
        }

        //把playerPawn从地图收回
        public bool ReclaimPawn(PawnKindDef kindDef,RaveCustomPawnUiData data)
        {
            if (kindDef == null)
                return false;

            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn) || pawn == null)
                return false;

            if (!pawn.Spawned)
                return false;
            data.Worker.UiReclaim(pawn);
            pawn.DeSpawn();
            if (Find.WorldPawns.Contains(pawn))
                Find.WorldPawns.RemovePawn(pawn);

            return true;
        }

        //查询playerPawn是否在地图上存活
        public bool IsSpawned(PawnKindDef kindDef)
        {
            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn))
                return false;
            if (pawn == null || pawn.Dead)
                return false;
            return pawn.Spawned;
        }

        public override void ExposeData()
        {
            // 解锁状态
            Scribe_Collections.Look(
                ref unlockStates,
                "specialPawnUnlockStates",
                LookMode.Def,
                LookMode.Value,
                ref tmpUnlockKeys,
                ref tmpUnlockValues
            );

            // 玩家 Pawn，Deep完整保存
            Scribe_Collections.Look(
                ref playerPawns,
                "playerPawns",
                LookMode.Def,
                LookMode.Deep,
                ref tmpPlayerPawnKeys,
                ref tmpPlayerPawnValues
            );
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockStates == null)
                    unlockStates = new Dictionary<PawnKindDef, bool>();
                if (playerPawns == null)
                    playerPawns = new Dictionary<PawnKindDef, Pawn>();
            }
        }
    }
}