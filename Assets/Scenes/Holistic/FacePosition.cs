using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mediapipe.Unity.Tutorial.Face
{
    public class FacePosition : MonoBehaviour
    {
        [SerializeField] private GameObject _facePosition;
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _pointMaterial;
        [SerializeField][Range(0.0001f, 0.1f)] private float _pointScale = 0.01f;
        [SerializeField] private float _lineWidth = 0.001f;
        [SerializeField] private float _hideDelay = 0.5f;
        [SerializeField][Range(0.0001f, 0.01f)] private float _positionChangeThreshold = 0.001f;
        [SerializeField][Range(0.01f, 0.5f)] private float _smoothTime = 0.1f;

        // 只关注10号关键点
        private static readonly int[] KeyPointIndices = { 10 };

        // 对象池（改为空对象，不显示）
        private Dictionary<int, GameObject> _landmarkPoints = new Dictionary<int, GameObject>();
        private List<LineRenderer> _connectionLines = new List<LineRenderer>();

        // 位置缓存和计时器
        private Dictionary<int, Vector3> _previousPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _previousRotations = new Dictionary<int, Quaternion>();
        private Dictionary<int, Vector3> _faceTargetPositions = new Dictionary<int, Vector3>();
        private float _lastUpdateTime = -1f;
        private bool _hasValidData = false;

        // 平滑移动的速度缓存（每个关键点一个）
        private Dictionary<int, Vector3> _faceMoveVelocities = new Dictionary<int, Vector3>();

        private void Start()
        {
            InitializeVisualization();
            HideVisualization();

            // 初始化位置和旋转缓存
            foreach (var index in KeyPointIndices)
            {
                _previousPositions[index] = Vector3.zero;
                _previousRotations[index] = Quaternion.identity;
                _faceTargetPositions[index] = Vector3.zero;
                _faceMoveVelocities[index] = Vector3.zero;
            }
        }

        private void Update()
        {
            // 更新插值移动
            UpdateSmoothMovement();

            // 检查是否需要隐藏可视化
            if (_hasValidData && Time.time - _lastUpdateTime > _hideDelay)
            {
                HideVisualization();
                _hasValidData = false;
            }
        }

        // 处理平滑移动
        private void UpdateSmoothMovement()
        {
            foreach (var index in KeyPointIndices)
            {
                if (_facePosition != null && _facePosition.activeSelf)
                {
                    // 使用临时变量解决ref问题
                    Vector3 currentVelocity = _faceMoveVelocities[index];
                    Vector3 currentPos = _facePosition.transform.localPosition;
                    Vector3 targetPos = _faceTargetPositions[index];

                    // 使用SmoothDamp进行平滑移动
                    Vector3 newPosition = Vector3.SmoothDamp(
                        currentPos,
                        targetPos,
                        ref currentVelocity,
                        _smoothTime
                    );

                    // 更新位置和速度缓存
                    _facePosition.transform.localPosition = newPosition;
                    _faceMoveVelocities[index] = currentVelocity;
                }
            }
        }

        private void InitializeVisualization()
        {
            // 创建空对象作为关键点（不显示）
            foreach (var index in KeyPointIndices)
            {
                // 改为创建空对象，而非球体
                var point = new GameObject($"FacePoint_{index}");
                point.transform.SetParent(transform);
                // 移除渲染相关组件，仅保留Transform用于位置计算
                // 不添加任何渲染组件，确保完全不可见

                _landmarkPoints[index] = point;

                if (_facePosition != null)
                {
                    _facePosition.SetActive(true);
                    _facePosition.transform.localPosition = Vector3.zero;
                    _facePosition.transform.localRotation = Quaternion.identity;
                    _faceTargetPositions[index] = Vector3.zero;
                }
            }
        }

        public void UpdateFaceLandmarks(IList<Vector3> landmarks)
        {
            // 检查数据是否有效
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 478 && HasAtLeastOneValidPoint(landmarks);

            if (!hasValidLandmarks)
            {
                _hasValidData = false;
                return;
            }

            // 更新状态和时间
            _hasValidData = true;
            _lastUpdateTime = Time.time;

            // 更新点和跟随对象
            UpdateKeyPointPositions(landmarks);
            ShowVisualization();
        }

        private bool HasAtLeastOneValidPoint(IList<Vector3> landmarks)
        {
            foreach (var index in KeyPointIndices)
            {
                if (index < landmarks.Count && IsValidLandmark(landmarks[index]))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateKeyPointPositions(IList<Vector3> landmarks)
        {
            foreach (var index in KeyPointIndices)
            {
                if (index >= landmarks.Count) continue;

                var position = landmarks[index];
                var pointObject = _landmarkPoints[index];

                if (IsValidLandmark(position))
                {
                    // 更新关键点位置（空对象仅用于位置计算）
                    pointObject.transform.localPosition = position;

                    // 缓存关键点的位置和旋转
                    _previousPositions[index] = position;
                    _previousRotations[index] = pointObject.transform.localRotation;

                    // 只更新目标位置，不直接设置_facePosition的位置
                    _faceTargetPositions[index] = position;

                    // 更新旋转（立即应用）
                    if (_facePosition != null)
                    {
                        _facePosition.transform.localRotation = pointObject.transform.localRotation;
                    }
                }
                else if (_previousPositions.ContainsKey(index))
                {
                    // 使用上次有效位置
                    pointObject.transform.localPosition = _previousPositions[index];
                    pointObject.transform.localRotation = _previousRotations[index];

                    // 只更新目标位置
                    _faceTargetPositions[index] = _previousPositions[index];

                    // 更新旋转（立即应用）
                    if (_facePosition != null)
                    {
                        _facePosition.transform.localRotation = _previousRotations[index];
                    }
                }
                else
                {
                    if (_facePosition != null)
                    {
                        _facePosition.SetActive(false);
                    }
                }
            }
        }

        private bool IsValidLandmark(Vector3 landmark)
        {
            return landmark != Vector3.zero &&
                   !float.IsNaN(landmark.x) &&
                   !float.IsNaN(landmark.y) &&
                   !float.IsNaN(landmark.z);
        }

        private void ShowVisualization()
        {
            // 关键点不显示，只控制_facePosition的显示
            if (_facePosition != null)
            {
                _facePosition.SetActive(true);
            }
        }

        private void HideVisualization()
        {
            // 关键点本身不显示，只隐藏_facePosition
            if (_facePosition != null)
            {
                _facePosition.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // 清理空对象
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) Destroy(point);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }

        // 移除点大小调整（因为点已不可见）
        private void OnValidate()
        {
            // 无需操作，因为关键点已不显示
        }
    }
}