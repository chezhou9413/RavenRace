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
        // 解锁状态
        private Dictionary<PawnKindDef, bool> unlockStates;

        // UI预览用的Pawn，懒加载，不存档，不进 WorldPawns
        private Dictionary<PawnKindDef, Pawn> previewPawns;

        // 给玩家的唯一Pawn实例，存档用 Reference（由 WorldPawns Deep 保存）
        private Dictionary<PawnKindDef, Pawn> playerPawns;

        // Scribe 临时列表
        private List<PawnKindDef> tmpUnlockKeys;
        private List<bool> tmpUnlockValues;
        private List<PawnKindDef> tmpPlayerPawnKeys;
        private List<Pawn> tmpPlayerPawnValues;

        // 有 RaveCustomPawnUiData 的 KindDef 缓存
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
            cachedSpecialKinds = new List<PawnKindDef>();
        }

        public static GameComponent_SpecialPawnUnlocks Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<GameComponent_SpecialPawnUnlocks>();
            }
        }

        public override void FinalizeInit()
        {
            // 扫描所有带扩展的 KindDef
            cachedSpecialKinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(d => d.GetModExtension<RaveCustomPawnUiData>() != null)
                .ToList();

            // previewPawns 不存档，每次加载都要从零开始
            // 直接清空字典即可，旧的 preview pawn 从未进过 WorldPawns，不需要额外清理
            previewPawns.Clear();

            foreach (PawnKindDef kindDef in cachedSpecialKinds)
            {
                // 补全解锁状态
                if (!unlockStates.ContainsKey(kindDef))
                    unlockStates[kindDef] = false;

                // playerPawn 只在第一次生成，之后复用同一个存档实例
                if (!playerPawns.ContainsKey(kindDef) || playerPawns[kindDef] == null)
                    playerPawns[kindDef] = GeneratePlayerPawn(kindDef);
            }
        }

        // ── Pawn 生成 ───────────────────────────────────────────

        /// <summary>
        /// 预览用 Pawn：只在内存里存活，不注册到 WorldPawns。
        /// 这样它永远不会成为 Free WorldPawn，不会污染黑衣人的关系抽池，
        /// 也不会触发 WorldPawnGC（因为根本不在里面）。
        /// </summary>
        private Pawn GeneratePreviewPawn(PawnKindDef kindDef)
        {
            Faction faction = FactionUtility.DefaultFactionFrom(kindDef.defaultFactionDef);
            Pawn pawn = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(
                    kindDef,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true,
                    // 预览 Pawn 不需要与任何人建立亲属关系
                    canGeneratePawnRelations: false
                )
            );
            // 不调用 PassToWorld —— 不进 WorldPawns，不占 situation，不影响任何叙事系统
            return pawn;
        }

        /// <summary>
        /// 交给玩家的正式 Pawn：注册到 WorldPawns 并用 ForcefullyKept 固定，
        /// 防止 WorldPawnGC 回收，同时不让它以 Free 身份参与关系抽池。
        /// </summary>
        private Pawn GeneratePlayerPawn(PawnKindDef kindDef)
        {
            Faction faction = FactionUtility.DefaultFactionFrom(kindDef.defaultFactionDef);
            Pawn pawn = PawnGenerator.GeneratePawn(
                new PawnGenerationRequest(
                    kindDef,
                    faction,
                    PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true,
                    // 不与外部 WorldPawn 建立关系，避免污染黑衣人关系抽池
                    canGeneratePawnRelations: false
                )
            );

            // 注册到 WorldPawns，由它负责 Deep 存档
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);

            // 用 ForcefullyKept 固定：
            //   1. 防止 WorldPawnGC 在长局中把它回收掉
            //   2. GetSituation() 会返回 ForceKept 而不是 Free，
            //      从而被排除在黑衣人/结局的关系抽池之外
            Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);

            return pawn;
        }

        // ── Tick ────────────────────────────────────────────────

        public override void GameComponentTick()
        {
            if (cachedSpecialKinds == null)
                return;
            if (Find.TickManager.TicksGame % 60 != 0)
                return;

            foreach (PawnKindDef kindDef in cachedSpecialKinds)
            {
                RaveCustomPawnUiData ext = kindDef.GetModExtension<RaveCustomPawnUiData>();
                if (ext == null) continue;

                SpecialPawnWorkerBase worker = ext.Worker;
                if (worker == null) continue;

                if (IsUnlocked(kindDef)) continue;

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

        // ── 解锁状态 API ─────────────────────────────────────────

        public bool IsUnlocked(PawnKindDef kindDef)
        {
            if (kindDef == null) return false;
            bool value;
            return unlockStates.TryGetValue(kindDef, out value) && value;
        }

        public void SetUnlocked(PawnKindDef kindDef, bool unlocked)
        {
            if (kindDef == null) return;
            unlockStates[kindDef] = unlocked;
        }

        public void Unlock(PawnKindDef kindDef) => SetUnlocked(kindDef, true);
        public void Lock(PawnKindDef kindDef) => SetUnlocked(kindDef, false);

        // ── 预览 Pawn API（懒加载）───────────────────────────────

        public Pawn GetPreviewPawn(PawnKindDef kindDef)
        {
            if (kindDef == null) return null;

            Pawn pawn;
            if (!previewPawns.TryGetValue(kindDef, out pawn) || pawn == null)
            {
                pawn = GeneratePreviewPawn(kindDef);
                previewPawns[kindDef] = pawn;
            }
            return pawn;
        }

        // ── 召唤 / 收回 API ──────────────────────────────────────

        /// <summary>把 playerPawn 生成到地图上交给玩家。</summary>
        public Pawn GivePawnToPlayer(PawnKindDef kindDef, IntVec3 spawnPos, Map map, RaveCustomPawnUiData data)
        {
            if (kindDef == null || map == null) return null;
            if (!spawnPos.IsValid || !spawnPos.InBounds(map)) return null;

            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn) || pawn == null) return null;
            if (pawn.Spawned || pawn.Dead) return null;

            GenSpawn.Spawn(pawn, spawnPos, map);
            pawn.SetFaction(Faction.OfPlayer);
            data.Worker.UiSummon(pawn);
            return pawn;
        }

        /// <summary>把 playerPawn 从地图收回。</summary>
        public bool ReclaimPawn(PawnKindDef kindDef, RaveCustomPawnUiData data)
        {
            if (kindDef == null) return false;

            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn) || pawn == null) return false;
            if (!pawn.Spawned) return false;

            data.Worker.UiReclaim(pawn);
            pawn.DeSpawn();
            return true;
        }

        /// <summary>查询 playerPawn 是否在地图上存活。</summary>
        public bool IsSpawned(PawnKindDef kindDef)
        {
            Pawn pawn;
            if (!playerPawns.TryGetValue(kindDef, out pawn)) return false;
            if (pawn == null || pawn.Dead) return false;
            return pawn.Spawned;
        }

        // ── 存档 ────────────────────────────────────────────────

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

            // playerPawn 用 Reference 模式：
            // WorldPawns 负责 Deep 序列化实体，这里只保存引用，避免重复注册 ID 冲突。
            // ForcefullyKeptPawns 本身由 WorldPawns.ExposeData() 负责存档，不需要我们额外处理。
            Scribe_Collections.Look(
                ref playerPawns,
                "playerPawns",
                LookMode.Def,
                LookMode.Reference,
                ref tmpPlayerPawnKeys,
                ref tmpPlayerPawnValues
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (unlockStates == null)
                    unlockStates = new Dictionary<PawnKindDef, bool>();
                if (playerPawns == null)
                    playerPawns = new Dictionary<PawnKindDef, Pawn>();

                // 加载后重新固定所有 playerPawn，
                // 因为 ForcefullyKeptPawns 是 HashSet<Pawn>，WorldPawns 存档时会把它保存下来，
                // 但以防万一（旧存档迁移等情况），这里补一遍确保完整性。
                foreach (Pawn pawn in playerPawns.Values)
                {
                    if (pawn != null && !pawn.Dead)
                        Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
                }
            }
        }
    }
}