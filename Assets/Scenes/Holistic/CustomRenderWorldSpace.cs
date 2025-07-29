using UnityEngine;
using Mediapipe.Unity.Tutorial.Hand;
using Mediapipe.Unity.Tutorial.Body;
using Meta.XR.EnvironmentDepth;
using System;
using Meta.XR;
using PassthroughCameraSamples;
using System.Collections.Generic;
using Mediapipe.Unity.Tutorial.Face;

namespace Mediapipe.Unity.Sample.Holistic
{
    public class CustomRenderWorldSpace : MonoBehaviour
    {
        [SerializeField] private EnvironmentDepthManager _depthManager;
        [SerializeField] private EnvironmentRaycastManager _environmentRaycastManager;

        [SerializeField] bool enableHand = true, enableBody = true, enableFace = true;
        [SerializeField] HandLine _handLine_Left, _handLine_Right;
        [SerializeField] BodyLine _bodyLine;
        [SerializeField] FaceLine _faceLine;
        [SerializeField] FacePosition _facePosition;
        [SerializeField] float _weight = 1280, _height = 960;

        // 使用HashSet优化点检查
        private HashSet<int> _facePointMap = new HashSet<int> { 10, 19, 61, 133, 152, 234, 291, 362, 454 };

        public void draw(NormalizedLandmarkList list, string position)
        {
            if (list == null) return;
            if (list.Landmark == null || list.Landmark.Count == 0) return;

            // 根据位置类型决定是否处理
            if (!ShouldProcessPosition(position)) return;

            // 创建合适大小的列表
            List<Vector3> positions = new List<Vector3>();

            if (position == "face")
            {
                // 为面部创建478个元素（即使大部分是零）
                for (int i = 0; i < 478; i++)
                {
                    positions.Add(Vector3.zero);
                }

                // 只计算PointMap中的点
                foreach (int pointIndex in _facePointMap)
                {
                    if (pointIndex >= list.Landmark.Count) continue;

                    var landmark = list.Landmark[pointIndex];
                    if (landmark == null) continue;

                    Vector2 uv = GetPreciseUV(landmark.X, landmark.Y);
                    positions[pointIndex] = ScreenUVToWorldPosition(uv);
                }
            }
            else
            {
                // 对于其他部位，处理所有点
                for (int i = 0; i < list.Landmark.Count; i++)
                {
                    if (list.Landmark[i] == null)
                    {
                        positions.Add(Vector3.zero);
                        continue;
                    }

                    Vector2 uv = GetPreciseUV(list.Landmark[i].X, list.Landmark[i].Y);
                    positions.Add(ScreenUVToWorldPosition(uv));
                }
            }

            // 更新可视化
            UpdateVisualization(position, positions);
        }

        private bool ShouldProcessPosition(string position)
        {
            return (position == "left_hand" && enableHand) ||
                   (position == "right_hand" && enableHand) ||
                   (position == "body" && enableBody) ||
                   (position == "face" && enableFace);
        }

        private void UpdateVisualization(string position, List<Vector3> positions)
        {
            switch (position)
            {
                case "body":
                    _bodyLine?.UpdateBodyLandmarks(positions);
                    break;
                case "left_hand":
                    _handLine_Left?.UpdateHandLandmarks(positions);
                    break;
                case "right_hand":
                    _handLine_Right?.UpdateHandLandmarks(positions);
                    break;
                case "face":
                    _facePosition?.UpdateFaceLandmarks(positions);
                    //_faceLine?.UpdateFaceLandmarks(positions);
                    break;
            }
        }

        private Vector2 GetPreciseUV(float x, float y)
        {
            return new Vector2(
                Mathf.Clamp01(x),
                1 - Mathf.Clamp01(y)
            );
        }

        private Vector3 ScreenUVToWorldPosition(Vector2 uv)
        {
            var cameraScreenPoint = new Vector2Int((int)(uv.x * _weight), (int)(uv.y * _height));
            var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(PassthroughCameraEye.Left, cameraScreenPoint);

            if (_environmentRaycastManager.Raycast(ray, out EnvironmentRaycastHit hitInfo))
            {
                return hitInfo.point;
            }

            return Vector3.zero;
        }
    }
}