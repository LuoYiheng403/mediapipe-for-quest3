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

        // ֻ��ע10�Źؼ���
        private static readonly int[] KeyPointIndices = { 10 };

        // ����أ���Ϊ�ն��󣬲���ʾ��
        private Dictionary<int, GameObject> _landmarkPoints = new Dictionary<int, GameObject>();
        private List<LineRenderer> _connectionLines = new List<LineRenderer>();

        // λ�û���ͼ�ʱ��
        private Dictionary<int, Vector3> _previousPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _previousRotations = new Dictionary<int, Quaternion>();
        private Dictionary<int, Vector3> _faceTargetPositions = new Dictionary<int, Vector3>();
        private float _lastUpdateTime = -1f;
        private bool _hasValidData = false;

        // ƽ���ƶ����ٶȻ��棨ÿ���ؼ���һ����
        private Dictionary<int, Vector3> _faceMoveVelocities = new Dictionary<int, Vector3>();

        private void Start()
        {
            InitializeVisualization();
            HideVisualization();

            // ��ʼ��λ�ú���ת����
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
            // ���²�ֵ�ƶ�
            UpdateSmoothMovement();

            // ����Ƿ���Ҫ���ؿ��ӻ�
            if (_hasValidData && Time.time - _lastUpdateTime > _hideDelay)
            {
                HideVisualization();
                _hasValidData = false;
            }
        }

        // ����ƽ���ƶ�
        private void UpdateSmoothMovement()
        {
            foreach (var index in KeyPointIndices)
            {
                if (_facePosition != null && _facePosition.activeSelf)
                {
                    // ʹ����ʱ�������ref����
                    Vector3 currentVelocity = _faceMoveVelocities[index];
                    Vector3 currentPos = _facePosition.transform.localPosition;
                    Vector3 targetPos = _faceTargetPositions[index];

                    // ʹ��SmoothDamp����ƽ���ƶ�
                    Vector3 newPosition = Vector3.SmoothDamp(
                        currentPos,
                        targetPos,
                        ref currentVelocity,
                        _smoothTime
                    );

                    // ����λ�ú��ٶȻ���
                    _facePosition.transform.localPosition = newPosition;
                    _faceMoveVelocities[index] = currentVelocity;
                }
            }
        }

        private void InitializeVisualization()
        {
            // �����ն�����Ϊ�ؼ��㣨����ʾ��
            foreach (var index in KeyPointIndices)
            {
                // ��Ϊ�����ն��󣬶�������
                var point = new GameObject($"FacePoint_{index}");
                point.transform.SetParent(transform);
                // �Ƴ���Ⱦ��������������Transform����λ�ü���
                // ������κ���Ⱦ�����ȷ����ȫ���ɼ�

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
            // ��������Ƿ���Ч
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 478 && HasAtLeastOneValidPoint(landmarks);

            if (!hasValidLandmarks)
            {
                _hasValidData = false;
                return;
            }

            // ����״̬��ʱ��
            _hasValidData = true;
            _lastUpdateTime = Time.time;

            // ���µ�͸������
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
                    // ���¹ؼ���λ�ã��ն��������λ�ü��㣩
                    pointObject.transform.localPosition = position;

                    // ����ؼ����λ�ú���ת
                    _previousPositions[index] = position;
                    _previousRotations[index] = pointObject.transform.localRotation;

                    // ֻ����Ŀ��λ�ã���ֱ������_facePosition��λ��
                    _faceTargetPositions[index] = position;

                    // ������ת������Ӧ�ã�
                    if (_facePosition != null)
                    {
                        _facePosition.transform.localRotation = pointObject.transform.localRotation;
                    }
                }
                else if (_previousPositions.ContainsKey(index))
                {
                    // ʹ���ϴ���Чλ��
                    pointObject.transform.localPosition = _previousPositions[index];
                    pointObject.transform.localRotation = _previousRotations[index];

                    // ֻ����Ŀ��λ��
                    _faceTargetPositions[index] = _previousPositions[index];

                    // ������ת������Ӧ�ã�
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
            // �ؼ��㲻��ʾ��ֻ����_facePosition����ʾ
            if (_facePosition != null)
            {
                _facePosition.SetActive(true);
            }
        }

        private void HideVisualization()
        {
            // �ؼ��㱾����ʾ��ֻ����_facePosition
            if (_facePosition != null)
            {
                _facePosition.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // ����ն���
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) Destroy(point);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }

        // �Ƴ����С��������Ϊ���Ѳ��ɼ���
        private void OnValidate()
        {
            // �����������Ϊ�ؼ����Ѳ���ʾ
        }
    }
}