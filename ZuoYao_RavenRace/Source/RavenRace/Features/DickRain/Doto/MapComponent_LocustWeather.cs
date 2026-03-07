using RavenRace.Features.DickRain.Doto.Data;
using RavenRace.Features.DickRain.Doto.RainJob;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Verse;

namespace RavenRace.Features.DickRain.Doto
{
    public class MapComponent_LocustWeather : MapComponent
    {
        private const int LocustCount = 30000;
        private const string TexturePath = "MIcs/dick";
        private const float SpawnDuration = 15f;
        private NativeArray<LocustData> _locustsNative;
        private Material _locustMat;
        private bool _dataInitialized;
        private bool _matsReady;
        private bool _wasActive;
        private float _spawnProgress;

        private const int HediffCheckInterval = 60;
        private const float SeverityGainOutdoors = 0.033f;
        private const float SeverityDecayIndoors = -0.008f;
        private const float MinSeverityToDecay = 0.05f;

        private HediffDef _arousalDef;
        private HediffDef ArousalDef => _arousalDef ??= HediffDef.Named("DickRain_Arousal");

        private static WeatherDef DickRainWeather =>
            DefDatabase<WeatherDef>.GetNamed("DickRain_Weather");

        private bool IsDickRainActive =>
            map.weatherManager.curWeather == DickRainWeather ||
            map.weatherManager.lastWeather == DickRainWeather;

        public MapComponent_LocustWeather(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            _locustsNative = new NativeArray<LocustData>(LocustCount, Allocator.Persistent);
            ResetToEdge();
            _dataInitialized = true;
        }

        private void ResetToEdge()
        {
            float mapH = map.Size.z;
            for (int i = 0; i < LocustCount; i++)
            {
                _locustsNative[i] = new LocustData
                {
                    position = new float2(Rand.Range(-5f, 0f), Rand.Range(0f, mapH)),
                    speed = Rand.Range(12f, 20f),
                    randomSeed = Rand.Range(0f, 1000f),
                };
            }
            _spawnProgress = 0f;
        }

        public override void MapComponentTick()
        {
            // 用 map.uniqueID 偏移，避免多地图同帧集中计算
            if ((Find.TickManager.TicksGame + map.uniqueID) % HediffCheckInterval != 0)
                return;

            bool rainActive = map.weatherManager.curWeather == DickRainWeather;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!pawn.RaceProps.Humanlike || pawn.Dead) continue;

                bool outdoors = !pawn.Position.Roofed(map);

                if (rainActive && outdoors)
                {
                    HealthUtility.AdjustSeverity(pawn, ArousalDef, SeverityGainOutdoors);
                }
                else
                {
                    Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(ArousalDef);
                    if (hediff != null && hediff.Severity > MinSeverityToDecay)
                    {
                        HealthUtility.AdjustSeverity(pawn, ArousalDef, SeverityDecayIndoors);
                    }
                }
            }
        }

        public override void MapComponentUpdate()
        {
            if (!IsDickRainActive)
            {
                if (_wasActive)
                {
                    ResetToEdge();
                    _matsReady = false;
                    _wasActive = false;
                }
                return;
            }
            if (!_dataInitialized) return;
            if (!_wasActive)
            {
                _wasActive = true;
                _spawnProgress = 0f;
            }
            if (!_matsReady)
            {
                var baseMat = MaterialPool.MatFrom(TexturePath, ShaderDatabase.Transparent);
                _locustMat = new Material(baseMat) { enableInstancing = true };
                _matsReady = true;
            }
            if (Find.TickManager.Paused) return;
            float dt = Time.deltaTime * Find.TickManager.TickRateMultiplier;
            if (dt <= 0f) return;
            if (_spawnProgress < 1f)
                _spawnProgress = Mathf.Clamp01(_spawnProgress + dt / SpawnDuration);

            LocustMovementJob moveJob = new LocustMovementJob
            {
                locusts = _locustsNative,
                deltaTime = dt,
                time = Time.realtimeSinceStartup,
                windDir = math.normalize(new float2(-1f, -0.2f)),
                mapW = map.Size.x,
                mapH = map.Size.z,
                activeCount = (int)(LocustCount * _spawnProgress),
            };
            JobHandle handle = moveJob.Schedule(LocustCount, 128);
            handle.Complete();
        }

        public override void MapComponentDraw()
        {
            if (!IsDickRainActive) return;
            if (!_dataInitialized || !_matsReady) return;

            int drawCount = (int)(LocustCount * _spawnProgress);
            if (drawCount <= 0) return;

            LocustRenderSystem.Draw(_locustsNative, _locustMat, drawCount);
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            DisposeData();
        }

        ~MapComponent_LocustWeather()
        {
            DisposeData();
        }

        private void DisposeData()
        {
            if (_locustsNative.IsCreated)
                _locustsNative.Dispose();
        }
    }
}