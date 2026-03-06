using System.Collections.Generic;
using Verse;

namespace RavenRace.Features.MiscSmallFeatures.AVRecording
{
    /// <summary>
    /// 地图级管理器：充当事件总线。
    /// 用于维护本图上所有的 AV 摄像机，并在接收到做爱结束事件时进行分发。
    /// </summary>
    public class MapComponent_AVManager : MapComponent
    {
        private HashSet<Building_AVCamera> activeCameras = new HashSet<Building_AVCamera>();

        public MapComponent_AVManager(Map map) : base(map)
        {
        }

        public void RegisterCamera(Building_AVCamera cam)
        {
            if (!activeCameras.Contains(cam))
            {
                activeCameras.Add(cam);
            }
        }

        public void DeregisterCamera(Building_AVCamera cam)
        {
            if (activeCameras.Contains(cam))
            {
                activeCameras.Remove(cam);
            }
        }

        /// <summary>
        /// 当做爱行为结束时由 Harmony Patch 调用。
        /// 寻找最合适（最近且满足条件）的一台摄像机进行记录。
        /// </summary>
        public void Notify_LovinFinished(Pawn actor, Pawn partner)
        {
            if (activeCameras.Count == 0 || actor == null) return;

            Building_AVCamera bestCamera = null;
            float shortestDistance = 9999f;

            // 遍历当前地图所有注册的摄像机
            foreach (var cam in activeCameras)
            {
                if (cam.CanRecordTarget(actor.Position))
                {
                    float dist = cam.Position.DistanceTo(actor.Position);
                    if (dist < shortestDistance)
                    {
                        shortestDistance = dist;
                        bestCamera = cam;
                    }
                }
            }

            // 只有距离最近的那一台摄像机会生成带子，防止多机器重复刷钱
            if (bestCamera != null)
            {
                bestCamera.RecordAndGenerateVideo(actor, partner);
            }
        }
    }
}