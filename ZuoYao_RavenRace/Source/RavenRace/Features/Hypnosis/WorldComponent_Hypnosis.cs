using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RavenRace.Features.Hypnosis
{
    public class WorldComponent_Hypnosis : WorldComponent
    {
        private Dictionary<int, List<int>> relationMap = new Dictionary<int, List<int>>();

        // 存档缓存变量
        private List<int> save_masters;
        private List<int> save_slaves_flattened; // 扁平化的所有奴隶ID
        private List<int> save_slaves_counts;    // 每个主人对应的奴隶数量

        public WorldComponent_Hypnosis(World world) : base(world) { }
        public static WorldComponent_Hypnosis Instance => Find.World.GetComponent<WorldComponent_Hypnosis>();

        public void AddBond(Pawn master, Pawn slave)
        {
            if (master == null || slave == null) return;
            if (!relationMap.ContainsKey(master.thingIDNumber))
                relationMap[master.thingIDNumber] = new List<int>();
            if (!relationMap[master.thingIDNumber].Contains(slave.thingIDNumber))
                relationMap[master.thingIDNumber].Add(slave.thingIDNumber);
        }

        public List<Pawn> GetSlaves(Pawn master)
        {
            if (master == null || !relationMap.ContainsKey(master.thingIDNumber)) return new List<Pawn>();
            List<int> ids = relationMap[master.thingIDNumber];
            List<Pawn> result = new List<Pawn>();
            foreach (int id in ids)
            {
                Pawn p = Find.World.worldPawns.AllPawnsAliveOrDead.FirstOrDefault(x => x.thingIDNumber == id)
                         ?? PawnsFinder.AllMaps_FreeColonistsSpawned.FirstOrDefault(x => x.thingIDNumber == id)
                         ?? PawnsFinder.AllMapsAndWorld_Alive.FirstOrDefault(x => x.thingIDNumber == id);
                if (p != null && !p.Destroyed) result.Add(p);
            }
            return result;
        }

        public bool IsMaster(Pawn pawn) => pawn != null && relationMap.ContainsKey(pawn.thingIDNumber) && relationMap[pawn.thingIDNumber].Count > 0;

        public override void ExposeData()
        {
            base.ExposeData();

            // 保存前：将字典转换为三个简单的 List
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                save_masters = new List<int>();
                save_slaves_flattened = new List<int>();
                save_slaves_counts = new List<int>();

                foreach (var kvp in relationMap)
                {
                    save_masters.Add(kvp.Key);
                    save_slaves_counts.Add(kvp.Value.Count);
                    save_slaves_flattened.AddRange(kvp.Value);
                }
            }

            // 执行保存
            Scribe_Collections.Look(ref save_masters, "save_masters", LookMode.Value);
            Scribe_Collections.Look(ref save_slaves_flattened, "save_slaves_flattened", LookMode.Value);
            Scribe_Collections.Look(ref save_slaves_counts, "save_slaves_counts", LookMode.Value);

            // 加载后：重建字典
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                relationMap.Clear();
                if (save_masters != null && save_slaves_counts != null && save_slaves_flattened != null)
                {
                    int currentSlaveIndex = 0;
                    for (int i = 0; i < save_masters.Count; i++)
                    {
                        int masterId = save_masters[i];
                        int count = save_slaves_counts[i];
                        List<int> slaves = new List<int>();

                        for (int j = 0; j < count; j++)
                        {
                            if (currentSlaveIndex < save_slaves_flattened.Count)
                            {
                                slaves.Add(save_slaves_flattened[currentSlaveIndex]);
                                currentSlaveIndex++;
                            }
                        }
                        relationMap[masterId] = slaves;
                    }
                }
            }
        }
    }
}