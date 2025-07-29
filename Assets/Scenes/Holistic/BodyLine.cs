using System.Collections.Generic;
using UnityEngine;

namespace Mediapipe.Unity.Tutorial.Body
{
    public class BodyLine : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _pointMaterial;
        [SerializeField][Range(0.0001f, 0.1f)] private float _pointScale = 0.01f;
        [SerializeField] private float _lineWidth = 0.001f;
        [SerializeField] private float _hideDelay = 0.5f;

        // 预定义连接映射
        private static readonly List<List<int>> BodyConnections = new List<List<int>> {
            new List<int> {1, 2, 3},             // 右臂
            new List<int> {4, 5, 6},             // 左臂
            new List<int> {7, 2, 0, 5, 8},       // 躯干上部
            new List<int> {9, 10},               // 嘴
            new List<int> {11, 13, 15},          // 右腿上部
            new List<int> {12, 14, 16},          // 左腿上部
            new List<int> {11, 12},              // 骨盆
            new List<int> {23, 24},              // 骨盆下部
            new List<int> {11, 23, 25, 27, 29, 31}, // 右腿完整
            new List<int> {12, 24, 26, 28, 30, 32}  // 左腿完整
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
            for (int i = 0; i < 33; i++)
            {
                
                    _previousPositions[i] = Vector3.zero;
                
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
            // 创建关键点（跳过不需要的点）
            for (int i = 0; i < 33; i++)
            {
                

                var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = $"BodyPoint_{i}";
                point.transform.SetParent(transform);
                point.transform.localScale = Vector3.one * _pointScale;
                point.GetComponent<Renderer>().material = _pointMaterial;
                Destroy(point.GetComponent<Collider>());

                _landmarkPoints[i] = point;
            }

            // 创建连接线
            foreach (var connection in BodyConnections)
            {
                var lineObj = new GameObject("BodyConnection");
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

        public void UpdateBodyLandmarks(IList<Vector3> landmarks)
        {
            // 检查数据是否有效
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 33 && HasAtLeastOneValidPoint(landmarks);

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
            foreach (var index in _landmarkPoints.Keys)
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
            foreach (var kvp in _landmarkPoints)
            {
                int index = kvp.Key;
                GameObject point = kvp.Value;

                if (index >= landmarks.Count) continue;

                var position = landmarks[index];

                if (IsValidLandmark(position))
                {
                    point.transform.localPosition = position;
                    point.SetActive(true);
                    _previousPositions[index] = position;
                }
                else if (_previousPositions.ContainsKey(index))
                {
                    // 使用上次有效位置
                    point.transform.localPosition = _previousPositions[index];
                    point.SetActive(true);
                }
                else
                {
                    point.SetActive(false);
                }

                // 设置颜色
                UpdatePointColor(point, index);
            }
        }

        private void UpdatePointColor(GameObject point, int index)
        {
            var renderer = point.GetComponent<Renderer>();
            if (renderer == null) return;

            // 根据身体部位设置颜色
            if (index >= 11 && index <= 16) // 腿部
            {
                renderer.material.color = index % 2 == 1 ? UnityEngine.Color.red : UnityEngine.Color.blue;
            }
            else if (index >= 23 && index <= 32) // 脚部
            {
                renderer.material.color = index % 2 == 0 ? UnityEngine.Color.green : UnityEngine.Color.yellow;
            }
            else // 上半身
            {
                renderer.material.color = UnityEngine.Color.white;
            }
        }

        private void UpdateConnectionLines()
        {
            for (int i = 0; i < BodyConnections.Count; i++)
            {
                var connection = BodyConnections[i];
                var lineRenderer = _connectionLines[i];
                bool hasValidPoints = true;

                // 更新线位置并检查有效性
                for (int j = 0; j < connection.Count; j++)
                {
                    int pointIndex = connection[j];

                    // 检查点是否有效
                    if (_landmarkPoints.TryGetValue(pointIndex, out GameObject point) &&
                        point != null &&
                        point.activeSelf)
                    {
                        lineRenderer.SetPosition(j, point.transform.localPosition);
                    }
                    else
                    {
                        hasValidPoints = false;
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

        // 兼容旧方法
        public void UpdateLinePositions(Vector3[] data)
        {
            if (data == null) return;
            UpdateBodyLandmarks(data);
        }
    }
}