using UnityEngine;
using System.Collections.Generic;

namespace Mediapipe.Unity.Tutorial.Hand
{
    public class HandLine : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _pointMaterial;
        [SerializeField][Range(0.0001f, 0.1f)] private float _pointScale = 0.01f;
        [SerializeField] private float _lineWidth = 0.001f;
        [SerializeField] private float _hideDelay = 0.5f;

        // �ֲ��ؼ������ӹ�ϵ
        private static readonly List<List<int>> HandConnections = new List<List<int>> {
            new List<int> {0, 1, 2, 3, 4},        // ��Ĵָ
            new List<int> {0, 5, 6, 7, 8},        // ʳָ
            new List<int> {0, 9, 10, 11, 12},     // ��ָ
            new List<int> {0, 13, 14, 15, 16},    // ����ָ
            new List<int> {0, 17, 18, 19, 20},    // Сָ
            new List<int> {5, 9, 13, 17}          // ���ƻ���
        };

        // �����
        private List<GameObject> _landmarkPoints = new List<GameObject>();
        private List<LineRenderer> _connectionLines = new List<LineRenderer>();

        // λ�û���ͼ�ʱ��
        private Vector3[] _previousPositions = new Vector3[21];
        private float _lastUpdateTime = -1f;
        private bool _hasValidData = false;

        private void Start()
        {
            InitializeVisualization();
            HideVisualization();

            // ��ʼ��λ�û���
            for (int i = 0; i < 21; i++)
            {
                _previousPositions[i] = Vector3.zero;
            }
        }

        private void Update()
        {
            // ����Ƿ���Ҫ���ؿ��ӻ�
            if (_hasValidData && Time.time - _lastUpdateTime > _hideDelay)
            {
                HideVisualization();
                _hasValidData = false;
            }
        }

        private void InitializeVisualization()
        {
            // �����ؼ���
            for (int i = 0; i < 21; i++)
            {
                var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = $"HandPoint_{i}";
                point.transform.SetParent(transform);
                point.transform.localScale = Vector3.one * _pointScale;
                point.GetComponent<Renderer>().material = _pointMaterial;
                Destroy(point.GetComponent<Collider>());

                _landmarkPoints.Add(point);
            }

            // ����������
            foreach (var connection in HandConnections)
            {
                var lineObj = new GameObject("HandConnection");
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

        public void UpdateHandLandmarks(IList<Vector3> landmarks)
        {
            // ��������Ƿ���Ч
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 21 && HasAtLeastOneValidPoint(landmarks);

            if (!hasValidLandmarks)
            {
                // û����Ч����ʱ�����״̬�����ֵ�ǰ���ӻ�������ʾ
                _hasValidData = false;
                return;
            }

            // ����״̬��ʱ��
            _hasValidData = true;
            _lastUpdateTime = Time.time;

            // ���µ����
            UpdateKeyPointPositions(landmarks);
            UpdateConnectionLines();
            ShowVisualization();
        }

        private bool HasAtLeastOneValidPoint(IList<Vector3> landmarks)
        {
            for (int i = 0; i < 21; i++)
            {
                if (i < landmarks.Count && IsValidLandmark(landmarks[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateKeyPointPositions(IList<Vector3> landmarks)
        {
            for (int i = 0; i < 21; i++)
            {
                if (i >= landmarks.Count) continue;

                var position = landmarks[i];

                if (IsValidLandmark(position))
                {
                    _landmarkPoints[i].transform.localPosition = position;
                    _landmarkPoints[i].SetActive(true);
                    _previousPositions[i] = position;
                }
                else
                {
                    // ʹ���ϴ���Чλ��
                    _landmarkPoints[i].transform.localPosition = _previousPositions[i];
                    _landmarkPoints[i].SetActive(true);
                }
            }
        }

        private void UpdateConnectionLines()
        {
            for (int i = 0; i < HandConnections.Count; i++)
            {
                var connection = HandConnections[i];
                var lineRenderer = _connectionLines[i];
                bool hasValidPoints = true;

                // ������λ�ò������Ч��
                for (int j = 0; j < connection.Count; j++)
                {
                    int pointIndex = connection[j];

                    if (pointIndex < _landmarkPoints.Count &&
                        _landmarkPoints[pointIndex] != null &&
                        _landmarkPoints[pointIndex].activeSelf)
                    {
                        lineRenderer.SetPosition(j, _landmarkPoints[pointIndex].transform.localPosition);
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
            // ����Ƿ�Ϊ��Ч�� (0,0,0) �� NaN
            return landmark != Vector3.zero &&
                   !float.IsNaN(landmark.x) &&
                   !float.IsNaN(landmark.y) &&
                   !float.IsNaN(landmark.z);
        }

        private void ShowVisualization()
        {
            foreach (var point in _landmarkPoints)
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
            foreach (var point in _landmarkPoints)
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
            // �������Ķ���
            foreach (var point in _landmarkPoints)
            {
                if (point != null) Destroy(point);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }

        // �༭������ʱ������Ĵ�С
        private void OnValidate()
        {
            if (!Application.isPlaying || _landmarkPoints == null) return;

            foreach (var point in _landmarkPoints)
            {
                if (point != null)
                {
                    point.transform.localScale = Vector3.one * _pointScale;
                }
            }
        }

        // ���ݾɷ�������ѡ��
        public void UpdateLinePositions(Vector3[] data)
        {
            if (data == null) return;
            UpdateHandLandmarks(data);
        }
    }
}