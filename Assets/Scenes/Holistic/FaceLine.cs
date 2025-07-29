using UnityEngine;
using System.Collections.Generic;

namespace Mediapipe.Unity.Tutorial.Face
{
    public class FaceLine : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _pointMaterial;
        [SerializeField][Range(0.0001f, 0.1f)] private float _pointScale = 0.01f;
        [SerializeField] private float _lineWidth = 0.001f;
        [SerializeField] private float _hideDelay = 0.5f;

        // 只关注的关键点索引
        private static readonly int[] KeyPointIndices = { 10, 19, 61, 133, 152, 234, 291, 362, 454 };

        // 关键点之间的连接关系
        private static readonly List<List<int>> FaceConnections = new List<List<int>> {
            new List<int> {10, 454, 152, 234, 10} // 面部轮廓
        };

        // 对象池
        private Dictionary<int, GameObject> _landmarkPoints = new Dictionary<int, GameObject>();
        private List<LineRenderer> _connectionLines = new List<LineRenderer>();

        // 位置缓存和计时器
        private Dictionary<int, Vector3> _previousPositions = new Dictionary<int, Vector3>();
        private float _lastUpdateTime = -1f;
        private bool _hasValidData = false;

        private void Start()
        {
            InitializeVisualization();
            HideVisualization();

            // 初始化位置缓存
            foreach (var index in KeyPointIndices)
            {
                _previousPositions[index] = Vector3.zero;
            }
        }

        private void Update()
        {
            // 检查是否需要隐藏可视化
            if (_hasValidData && Time.time - _lastUpdateTime > _hideDelay)
            {
                HideVisualization();
                _hasValidData = false;
            }
        }

        private void InitializeVisualization()
        {
            // 创建关键点
            foreach (var index in KeyPointIndices)
            {
                var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = $"FacePoint_{index}";
                point.transform.SetParent(transform);
                point.transform.localScale = Vector3.one * _pointScale;
                point.GetComponent<Renderer>().material = _pointMaterial;
                Destroy(point.GetComponent<Collider>());

                _landmarkPoints[index] = point;
            }

            // 创建连接线
            foreach (var connection in FaceConnections)
            {
                var lineObj = new GameObject("FaceConnection");
                lineObj.transform.SetParent(transform);

                var lineRenderer = lineObj.AddComponent<LineRenderer>();
                lineRenderer.positionCount = connection.Count;
                lineRenderer.startWidth = _lineWidth;
                lineRenderer.endWidth = _lineWidth;
                lineRenderer.material = _lineMaterial;
                lineRenderer.useWorldSpace = false;
                lineRenderer.loop = false;
                lineRenderer.generateLightingData = true;

                _connectionLines.Add(lineRenderer);
            }
        }

        public void UpdateFaceLandmarks(IList<Vector3> landmarks)
        {
            // 检查数据是否有效
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 478 && HasAtLeastOneValidPoint(landmarks);

            if (!hasValidLandmarks)
            {
                // 没有有效数据时，标记状态但保持当前可视化短暂显示
                _hasValidData = false;
                return;
            }

            // 更新状态和时间
            _hasValidData = true;
            _lastUpdateTime = Time.time;

            // 更新点和线
            UpdateKeyPointPositions(landmarks);
            UpdateConnectionLines();
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

                if (IsValidLandmark(position))
                {
                    _landmarkPoints[index].transform.localPosition = position;
                    _landmarkPoints[index].SetActive(true);
                    _previousPositions[index] = position;
                }
                else if (_previousPositions.ContainsKey(index))
                {
                    // 使用上次有效位置
                    _landmarkPoints[index].transform.localPosition = _previousPositions[index];
                    _landmarkPoints[index].SetActive(true);
                }
                else
                {
                    _landmarkPoints[index].SetActive(false);
                }
            }
        }

        private void UpdateConnectionLines()
        {
            for (int i = 0; i < FaceConnections.Count; i++)
            {
                var connection = FaceConnections[i];
                var lineRenderer = _connectionLines[i];
                bool hasValidPoints = true;

                // 更新线位置并检查有效性
                for (int j = 0; j < connection.Count; j++)
                {
                    int pointIndex = connection[j];

                    if (_landmarkPoints.TryGetValue(pointIndex, out GameObject point) &&
                        point != null &&
                        point.activeSelf)
                    {
                        lineRenderer.SetPosition(j, point.transform.localPosition);
                    }
                    else
                    {
                        hasValidPoints = false;
                        // 不需要break，继续设置位置但标记无效
                    }
                }

                lineRenderer.enabled = hasValidPoints;
            }
        }

        private bool IsValidLandmark(Vector3 landmark)
        {
            // 检查是否为无效点 (0,0,0) 或 NaN
            return landmark != Vector3.zero &&
                   !float.IsNaN(landmark.x) &&
                   !float.IsNaN(landmark.y) &&
                   !float.IsNaN(landmark.z);
        }

        private void ShowVisualization()
        {
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) point.SetActive(true);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) line.enabled = true;
            }
        }

        private void HideVisualization()
        {
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) point.SetActive(false);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) line.enabled = false;
            }
        }

        private void OnDestroy()
        {
            // 清理创建的对象
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) Destroy(point);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }

        // 编辑器更新时调整点的大小
        private void OnValidate()
        {
            if (!Application.isPlaying || _landmarkPoints == null) return;

            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null)
                {
                    point.transform.localScale = Vector3.one * _pointScale;
                }
            }
        }
    }
}