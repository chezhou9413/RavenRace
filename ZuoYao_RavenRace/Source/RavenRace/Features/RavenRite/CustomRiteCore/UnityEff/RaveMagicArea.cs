using ChezhouLib.ALLmap;
using RimWorld;
using System.Collections;
using UnityEngine;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.UnityEff
{
   
    //延迟销毁协程组件
    public class MagicAreaAutoDestroy : MonoBehaviour
    {
        //开始倒计时，倒计时结束后销毁挂载的GameObject。
        public void BeginDestroy(float delaySeconds)
        {
            StartCoroutine(DestroyAfterDelay(delaySeconds));
        }

        private IEnumerator DestroyAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            Destroy(gameObject);
        }
    }

    public class RaveMagicArea
    {
        //旋转速度
        private const float RotateSpeedSlow = 15f;  // area0 最慢
        private const float RotateSpeedMedium = 40f;  // area1 中等
        private const float RotateSpeedFast = 90f;  // area2 / area21 最快
        //销毁延迟秒数
        private const float DestroyDelaySeconds = 10f;
        //父物体
        private GameObject magicArea;
        //子物体缓存（Spawn 时查找一次，之后直接用引用)
        private GameObject area0;
        private GameObject area1;
        private GameObject area2;
        private GameObject area21;
        private GameObject particle;
        //阶段记录
        private int lastStage = -1;
        //是否已触发销毁流程
        private bool destroyScheduled = false;
        //在世界坐标worldPos处生成魔法圈。
        public void Spawn(Vector3 worldPos)
        {
            if (magicArea != null)
            {
                return; //已存在，不重复创建
            }

            worldPos.y = 7f;

            magicArea = Object.Instantiate(
                abDatabase.prefabDataBase["MagicArea"],
                worldPos,
                Quaternion.Euler(90f, 0f, 0f));

            //挂载自销毁组件
            magicArea.AddComponent<MagicAreaAutoDestroy>();
            //缓存子物体引用，名字拼错时返回null而不是崩溃
            area0 = FindChild("MagicArea0");
            area1 = FindChild("MagicArea1");
            area2 = FindChild("MagicArea2");
            area21 = FindChild("MagicArea21");
            particle = FindChild("MagicArea_Particle");
            //初始状态：只显示 area0，其余全部隐藏
            SetActive(area0, true);
            SetActive(area1, false);
            SetActive(area2, false);
            SetActive(area21, false);
            SetActive(particle, false);
        }

        //每帧（或每 tick）调用，传入当前进度 0~1。
        //负责切换显示阶段 + 驱动旋转动画 + 缩放。
        public void Update(float progress)
        {
            if (magicArea == null)
            {
                return;
            }

            int stage = CalcStage(progress);
            if (stage != lastStage)
            {
                ApplyStage(stage);
                lastStage = stage;
            }

            //触发延迟销毁，不再旋转和缩放，直接返回
            if (stage == 3)
            {
                return;
            }
            Rotate(area0, RotateSpeedSlow, false);
            Rotate(area1, RotateSpeedMedium, true);
            Rotate(area2, RotateSpeedFast, false);
            Rotate(area21, RotateSpeedFast, false);
            magicArea.transform.localScale = Vector3.one * (1f + (progress*2));
        }

        //立即销毁父物体并清空所有引用。
        public void Destroy()
        {
            //已触发延迟销毁流程（粒子正在播放），让协程自然结束，不强行打断
            if (destroyScheduled)
            {
                return;
            }

            if (magicArea != null)
            {
                Object.Destroy(magicArea);
            }

            ClearReferences();
        }
        //辅助方法
        private int CalcStage(float progress)
        {
            if (progress >= 1.0f) return 3;
            if (progress > 0.7f) return 2;
            if (progress > 0.3f) return 1;
            return 0;
        }

        //根据阶段编号切换各子物体的激活状态。
        private void ApplyStage(int stage)
        {
            if (stage == 0)
            {
                SetActive(area0, true);
                SetActive(area1, false);
                SetActive(area2, false);
                SetActive(area21, false);
                SetActive(particle, false);
            }
            else if (stage == 1)
            {
                SetActive(area0, true);
                SetActive(area1, true);
                SetActive(area2, false);
                SetActive(area21, false);
                SetActive(particle, false);
            }
            else if (stage == 2)
            {
                SetActive(area0, true);
                SetActive(area1, true);
                SetActive(area2, true);
                SetActive(area21, true);
                SetActive(particle, false);
            }
            else
            {
                SetActive(area0, false);
                SetActive(area1, false);
                SetActive(area2, false);
                SetActive(area21, false);
                SetActive(particle, true);

                ScheduleDestroy();
            }
        }

        //通过挂载在GameObject上的MagicAreaAutoDestroy组件启动延迟销毁协程。
        private void ScheduleDestroy()
        {
            if (destroyScheduled)
            {
                return;
            }
            destroyScheduled = true;
            MagicAreaAutoDestroy autoDestroy = magicArea.GetComponent<MagicAreaAutoDestroy>();
            if (autoDestroy != null)
            {
                autoDestroy.BeginDestroy(DestroyDelaySeconds);
            }
            else
            {
                Object.Destroy(magicArea);
            }
        }

        //对目标物体做旋转，speed单位为度/秒。
        //物体为null或未激活时跳过。
        private void Rotate(GameObject target, float speed,bool fan)
        {
            if (target == null)
            {
                return;
            }
            if (!target.activeSelf)
            {
                return;
            }
            if (fan) 
            {
                target.transform.Rotate(0f, 0f, -(speed * Time.deltaTime), Space.Self);
            }
            else
            {
                target.transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self);
            }
        }

        //在父物体下按名字查找子物体，找不到时打印警告并返回null。
        private GameObject FindChild(string childName)
        {
            if (magicArea == null)
            {
                return null;
            }

            Transform found = magicArea.transform.Find(childName);

            if (found == null)
            {
                return null;
            }
            return found.gameObject;
        }

        private void SetActive(GameObject target, bool active)
        {
            if (target == null)
            {
                return;
            }
            target.SetActive(active);
        }
        private void ClearReferences()
        {
            magicArea = null;
            area0 = null;
            area1 = null;
            area2 = null;
            area21 = null;
            particle = null;
            lastStage = -1;
            destroyScheduled = false;
        }
    }
}