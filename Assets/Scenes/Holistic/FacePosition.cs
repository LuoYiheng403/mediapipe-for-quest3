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

        private static readonly int[] KeyPointIndices = { 10 };

        private Dictionary<int, GameObject> _landmarkPoints = new Dictionary<int, GameObject>();
        private List<LineRenderer> _connectionLines = new List<LineRenderer>();

        private Dictionary<int, Vector3> _previousPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, Quaternion> _previousRotations = new Dictionary<int, Quaternion>();
        private Dictionary<int, Vector3> _faceTargetPositions = new Dictionary<int, Vector3>();
        private float _lastUpdateTime = -1f;
        private bool _hasValidData = false,_lasthasValidData=false;
        private bool _isVisualizationActive = false; // 新增：追踪可视化状态

        private Dictionary<int, Vector3> _faceMoveVelocities = new Dictionary<int, Vector3>();

        private void Start()
        {
            InitializeVisualization();
            SetVisualizationActive(false); // 使用新方法设置初始状态

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

            UpdateSmoothMovement();
            if(_hasValidData&&!_lasthasValidData)
            {
                _lasthasValidData = true;
                SetVisualizationActive(true);
                
            }
            else if(!_hasValidData&&_lasthasValidData&& Time.time - _lastUpdateTime > _hideDelay)
            {
                SetVisualizationActive(false); // 使用新方法隐藏
                _lasthasValidData = false;
                
            }
            else
            {
                return;
            }
            
        }

        private void UpdateSmoothMovement()
        {
            foreach (var index in KeyPointIndices)
            {
                if (_facePosition != null)
                {
                    Vector3 currentVelocity = _faceMoveVelocities[index];
                    Vector3 currentPos = _facePosition.transform.localPosition;
                    Vector3 targetPos = _faceTargetPositions[index];

                    Vector3 newPosition = Vector3.SmoothDamp(
                        currentPos,
                        targetPos,
                        ref currentVelocity,
                        _smoothTime
                    );

                    //_facePosition.transform.localPosition = targetPos;
                    _facePosition.transform.localPosition = newPosition;
                    _faceMoveVelocities[index] = currentVelocity;
                }
            }
        }

        private void InitializeVisualization()
        {
            foreach (var index in KeyPointIndices)
            {
                var point = new GameObject($"FacePoint_{index}");
                point.transform.SetParent(transform);
                _landmarkPoints[index] = point;
            }
        }

        public void UpdateFaceLandmarks(IList<Vector3> landmarks)
        {
            bool hasValidLandmarks = landmarks != null && landmarks.Count >= 478 && HasAtLeastOneValidPoint(landmarks);

            if (!hasValidLandmarks)
            {
                _hasValidData = false;
                //_lasthasValidData = false;
                return;
            }

            _hasValidData = true;
            _lastUpdateTime = Time.time;

            UpdateKeyPointPositions(landmarks);
            //SetVisualizationActive(true); // 使用新方法显示
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
                    pointObject.transform.localPosition = position;
                    _previousPositions[index] = position;
                    _previousRotations[index] = pointObject.transform.localRotation;
                    _faceTargetPositions[index] = position;

                    if (_facePosition != null)
                    {
                        _facePosition.transform.localRotation = pointObject.transform.localRotation;
                    }
                }
                else if (_previousPositions.ContainsKey(index))
                {
                    pointObject.transform.localPosition = _previousPositions[index];
                    pointObject.transform.localRotation = _previousRotations[index];
                    _faceTargetPositions[index] = _previousPositions[index];

                    if (_facePosition != null)
                    {
                        _facePosition.transform.localRotation = _previousRotations[index];
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

        // 新增：统一管理可视化激活状态
        private void SetVisualizationActive(bool active)
        {
            if (_facePosition == null || _isVisualizationActive == active)
                return;

            _facePosition.SetActive(active);
            _isVisualizationActive = active;
        }

        private void OnDestroy()
        {
            foreach (var point in _landmarkPoints.Values)
            {
                if (point != null) Destroy(point);
            }

            foreach (var line in _connectionLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }
    }
}