using RimWorld;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.MiscSmallFeatures.AVRecording
{
    /// <summary>
    /// AV全息摄影机建筑。
    /// 不主动轮询扫描，被动接收事件总线通知，性能开销几乎为零。
    /// 结合原版房间系统判断产出质量。
    /// </summary>
    public class Building_AVCamera : Building
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickComp;

        // 拍摄半径（与XML中的 specialDisplayRadius 保持一致）
        public const float RecordRadius = 12.9f;

        // 冷却时间，防止同时有多个行为导致极短时间内疯狂产出 (设为游戏时间1小时)
        private int cooldownTicksLeft = 0;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            flickComp = GetComp<CompFlickable>();

            // 注册到地图管理器
            map.GetComponent<MapComponent_AVManager>()?.RegisterCamera(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            // 取消注册
            Map?.GetComponent<MapComponent_AVManager>()?.DeregisterCamera(this);
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cooldownTicksLeft, "cooldownTicksLeft", 0);
        }

        /// <summary>
        /// [修复编译错误] RimWorld 1.6 中 ThingWithComps.Tick() 被设为了 protected
        /// </summary>
        protected override void Tick()
        {
            base.Tick();
            if (cooldownTicksLeft > 0)
            {
                cooldownTicksLeft--;
            }
        }

        /// <summary>
        /// 检查该摄像机当前是否可以拍摄指定目标
        /// </summary>
        public bool CanRecordTarget(IntVec3 targetPos)
        {
            // 1. 状态检查：有电、开启、无冷却
            if (powerComp != null && !powerComp.PowerOn) return false;
            if (flickComp != null && !flickComp.SwitchIsOn) return false;
            if (cooldownTicksLeft > 0) return false;

            // 2. 距离检查
            if (this.Position.DistanceTo(targetPos) > RecordRadius) return false;

            // 3. 视距检查（核心！这样墙壁和门就会自动阻挡视线，实现房间级别的物理限制）
            if (!GenSight.LineOfSight(this.Position, targetPos, this.Map, true, null, 0, 0)) return false;

            return true;
        }

        /// <summary>
        /// 执行拍摄并生成录像带。
        /// 根据所在的房间类型决定生成的录像带品质。
        /// </summary>
        public void RecordAndGenerateVideo(Pawn actor, Pawn partner)
        {
            // 重置冷却时间（2500 ticks = 游戏时间1小时）
            cooldownTicksLeft = 2500;

            // 判断是否在专属的 AV摄影房 内
            bool isPremiumStudio = false;
            Room room = this.GetRoom();
            if (room != null && room.Role != null && room.Role.defName == "Raven_RoomRole_AVStudio")
            {
                isPremiumStudio = true;
            }

            // 根据房间类型选择生成的 Def
            string defName = isPremiumStudio ? "Raven_Item_AVRecord_Premium" : "Raven_Item_AVRecord";
            ThingDef videoDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (videoDef == null) return;

            Thing video = ThingMaker.MakeThing(videoDef);

            // 初始化自定义组件的骚话文本
            CompAVRecord comp = video.TryGetComp<CompAVRecord>();
            if (comp != null)
            {
                comp.InitializeRecord(actor, partner, isPremiumStudio);
            }

            // 在摄影机的交互点或旁边生成物品
            GenPlace.TryPlaceThing(video, this.InteractionCell, this.Map, ThingPlaceMode.Near);

            // 发送提示信件
            string msg = isPremiumStudio
                ? $"专业影棚发力！{actor.LabelShort} 刚才那令人血脉贲张的极乐过程被摄影机完美记录，并渲染成了价值连城的典藏版情色大片！"
                : $"{actor.LabelShort} 刚才的极乐过程被摄影机偷偷记录下来了。";

            Messages.Message(msg, video, MessageTypeDefOf.PositiveEvent);

            // 视觉特效与音效
            FleckMaker.ThrowMicroSparks(this.DrawPos, this.Map);
            SoundDefOf.TinyBell.PlayOneShot(new TargetInfo(this.Position, this.Map));
        }
    }
}