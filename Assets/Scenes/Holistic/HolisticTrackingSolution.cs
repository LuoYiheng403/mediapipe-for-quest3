// Copyright (c) 2021 homuler
// ʹ��MIT���֤����ϸ�������LICENSE�ļ���https://opensource.org/licenses/MIT�鿴

using PassthroughCameraSamples;  // ����XRֱͨ������ܵ������ռ�
using System.Collections;          // �ṩIEnumerator�Ȼ���������
using UnityEngine;                 // Unity������������ռ�
using UnityEngine.Rendering;       // �ṩAsyncGPUReadback����Ⱦ��ع���

namespace Mediapipe.Unity.Sample.Holistic
{
    // ȫ��׷�ٽ���������̳���LegacySolutionRunner���ָࣨ��ʹ��HolisticTrackingGraph��
    public class HolisticTrackingSolution : LegacySolutionRunner<HolisticTrackingGraph>
    {
        // ========== ���л��ֶΣ���Unity�༭���п����ã�==========
        [SerializeField] private CustomRenderWorldSpace _customRenderWorldSpace;
        //[SerializeField] private WebCamTextureManager _webCamTextureManager;  // ��������ͷ���������
        //[SerializeField] private RectTransform _worldAnnotationArea;          // 3D�ռ��ע����ľ��α任���
        //[SerializeField] private DetectionAnnotationController _poseDetectionAnnotationController;       // ���Ƽ���ע������
        //[SerializeField] private HolisticLandmarkListAnnotationController _holisticAnnotationController; // ȫ��ؼ����ע������
        //[SerializeField] private PoseWorldLandmarkListAnnotationController _poseWorldLandmarksAnnotationController; // 3D���ƹؼ����ע������
        //[SerializeField] private MaskAnnotationController _segmentationMaskAnnotationController;        // �ָ������ע������
        //[SerializeField] private NormalizedRectAnnotationController _poseRoiAnnotationController;       // ����ROI�����ע������

        // ����֡�أ����ڸ�Ч���������ڴ棩
        private Experimental.TextureFramePool _textureFramePool;

        // ========== ģ���������ԣ�����graphRunner��==========

        // ģ�͸��Ӷ�����
        public HolisticTrackingGraph.ModelComplexity modelComplexity
        {
            get => graphRunner.modelComplexity;  // ��ȡ��ǰģ�͸��Ӷ�
            set => graphRunner.modelComplexity = value;  // ����ģ�͸��Ӷȣ�Lite/Full/Heavy��
        }

        // �Ƿ�ƽ���ؼ���
        public bool smoothLandmarks
        {
            get => graphRunner.smoothLandmarks;
            set => graphRunner.smoothLandmarks = value;
        }

        // �Ƿ��Ż��沿�ؼ���
        public bool refineFaceLandmarks
        {
            get => graphRunner.refineFaceLandmarks;
            set => graphRunner.refineFaceLandmarks = value;
        }

        // �Ƿ����÷ָ�
        public bool enableSegmentation
        {
            get => graphRunner.enableSegmentation;
            set => graphRunner.enableSegmentation = value;
        }

        // �Ƿ�ƽ���ָ���
        public bool smoothSegmentation
        {
            get => graphRunner.smoothSegmentation;
            set => graphRunner.smoothSegmentation = value;
        }

        // ��С������Ŷ���ֵ
        public float minDetectionConfidence
        {
            get => graphRunner.minDetectionConfidence;
            set => graphRunner.minDetectionConfidence = value;
        }

        // ��С�������Ŷ���ֵ
        public float minTrackingConfidence
        {
            get => graphRunner.minTrackingConfidence;
            set => graphRunner.minTrackingConfidence = value;
        }

        // ========== �������ڷ��� ==========

        // ֹͣ�����������д���෽����
        public override void Stop()
        {
            base.Stop();  // ���û���ֹͣ�߼�
            _textureFramePool?.Dispose();  // ��ȫ�ͷ�����֡����Դ
            _textureFramePool = null;      // ������ñ�����������
        }

        // ========== ����Э�̣����������ѭ�� ==========
        protected override IEnumerator Run()
        {
            // ��ʼ��MediaPipeͼ���ȴ���ɣ��첽������
            var graphInitRequest = graphRunner.WaitForInit(runningMode);

            // ��ImageSourceProvider��ȡͼ��Դʵ��
            var imageSource = ImageSourceProvider.ImageSource;

            // ����ͼ��Դ���ţ��첽������
            yield return imageSource.Play();

            // ���ͼ��Դ�Ƿ�׼������
            if (!imageSource.isPrepared)
            {
                Debug.LogError("ͼ��Դ����ʧ�ܣ��˳�...");
                yield break;  // ��ǰ��ֹЭ��
            }

            // ��������֡�أ�RGBA32��ʽ��
            // �����������ȡ��߶ȡ���ʽ��������
            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth,
                imageSource.textureHeight,
                TextureFormat.RGBA32,
                10  // Ԥ����10������֡
            );

            // ��ʼ��UI��Ļ���������ͼ��Դ�����ߴ磩
            //screen.Initialize(imageSource);

            // ����3D��ע������ת������ͼ��Դ��ת��
            // Reverse()��ͼ��Դ��תת��Ϊ����ռ���ת
            //_worldAnnotationArea.localEulerAngles = imageSource.rotation.Reverse().GetEulerAngles();

            // �ȴ�ͼ��ʼ����ɣ�yieldֱ���첽������ɣ�
            yield return graphInitRequest;

            // ����ʼ���Ƿ����
            if (graphInitRequest.isError)
            {
                Debug.LogError(graphInitRequest.error);
                yield break;
            }

            // ===== �첽ģʽ���� =====
            if (!runningMode.IsSynchronous())  // ������첽����ģʽ
            {
                // ע��������������¼�������
                //graphRunner.OnPoseDetectionOutput += OnPoseDetectionOutput;
                graphRunner.OnFaceLandmarksOutput += OnFaceLandmarksOutput;
                graphRunner.OnPoseLandmarksOutput += OnPoseLandmarksOutput;
                graphRunner.OnLeftHandLandmarksOutput += OnLeftHandLandmarksOutput;
                graphRunner.OnRightHandLandmarksOutput += OnRightHandLandmarksOutput;
                //graphRunner.OnPoseWorldLandmarksOutput += OnPoseWorldLandmarksOutput;
                //graphRunner.OnSegmentationMaskOutput += OnSegmentationMaskOutput;
                //graphRunner.OnPoseRoiOutput += OnPoseRoiOutput;
            }

            // ===== ��ʼ����ע������ =====
            // Ϊÿ������������ͼ��Դ�ο�
            //SetupAnnotationController(_poseDetectionAnnotationController, imageSource);
            //SetupAnnotationController(_holisticAnnotationController, imageSource);
            //SetupAnnotationController(_poseWorldLandmarksAnnotationController, imageSource);
            //SetupAnnotationController(_segmentationMaskAnnotationController, imageSource);

            // �ر��ʼ���ָ��������������Ҫ֪������ߴ磩
            //_segmentationMaskAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            //SetupAnnotationController(_poseRoiAnnotationController, imageSource);

            // ����MediaPipeͼ���У�����ͼ��Դ���ã�
            graphRunner.StartRun(imageSource);

            // ===== ׼��GPU�첽��ȡ =====
            AsyncGPUReadbackRequest req = default;  // GPU��ȡ����ṹ��
            var waitUntilReqDone = new WaitUntil(() => req.done);  // �ȴ��������������

            // ����Ƿ�֧��GPU���٣���Android+OpenGLES��GPU��Դ���ã�
            var canUseGpuImage = graphRunner.configType == GraphRunner.ConfigType.OpenGLES &&
                               GpuManager.GpuResources != null;

            // ��ȡOpenGL�����ģ����֧��GPU���٣�
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            // ===== ������ѭ�� =====
            while (true)  // ����ѭ��ֱ�����ⲿֹͣ
            {
                // ��ͣ״̬����
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);  // �ȴ�ֱ��ȡ����ͣ
                }

                // �ӳ��л�ȡ��������֡
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();  // �ȴ�֡����
                    continue;  // ������ǰ����
                }

                // === ����ǰͼ���Ƶ�����֡ ===
                if (canUseGpuImage)  // GPU����·��
                {
                    // �ȴ���ǰ֡��Ⱦ���
                    yield return new WaitForEndOfFrame();

                    // ֱ�Ӵ�GPU��ȡ��������CPU���ƣ�
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture());
                }
                else  // CPU·��
                {
                    // �����첽GPU��ȡ����
                    // ������Դ����mipmap���𡢴�ֱ��ת
                    req = textureFrame.ReadTextureAsync(
                        imageSource.GetCurrentTexture(),
                        false,
                        imageSource.isVerticallyFlipped
                    );

                    // �ȴ���ȡ���
                    yield return waitUntilReqDone;

                    // ����ȡ����
                    if (req.hasError)
                    {
                        Debug.LogWarning("��ͼ��Դ��ȡ����ʧ��");
                        yield return new WaitForEndOfFrame();
                        continue;
                    }
                }

                // ������֡����MediaPipe������
                // glContext��֧��GPUʱ�ṩ����������
                graphRunner.AddTextureFrameToInputStream(textureFrame, glContext);

                // === ͬ������ģʽ ===
                if (runningMode.IsSynchronous())
                {
                    // ������Ļ��ʾ��ʹ�õ�ǰ����֡��
                    //screen.ReadSync(textureFrame);

                    // �ȴ���һ�����������첽����
                    var task = graphRunner.WaitNextAsync();
                    yield return new WaitUntil(() => task.IsCompleted);

                    // ��ȡ���
                    var result = task.Result;

                    // �����������б�ע
                    //_poseDetectionAnnotationController.DrawNow(result.poseDetection);
                    //_holisticAnnotationController.DrawNow(
                    //    result.faceLandmarks,
                    //    result.poseLandmarks,
                    //    result.leftHandLandmarks,
                    //    result.rightHandLandmarks
                    //);

                    _customRenderWorldSpace.draw(result.poseLandmarks,"body");
                    _customRenderWorldSpace.draw(result.leftHandLandmarks, "left_hand");
                    _customRenderWorldSpace.draw(result.rightHandLandmarks, "right_hand");
                    _customRenderWorldSpace.draw(result.faceLandmarks, "face");

                    //_poseWorldLandmarksAnnotationController.DrawNow(result.poseWorldLandmarks);
                    //_segmentationMaskAnnotationController.DrawNow(result.segmentationMask);
                    //_poseRoiAnnotationController.DrawNow(result.poseRoi);

                    // �ͷŷָ�������Դ��������ڣ�
                    result.segmentationMask?.Dispose();
                }
            }  // ����whileѭ��
        }  // ����RunЭ��

        // ========== �첽ģʽ�¼������� ==========

        // ���Ƽ����������
        //private void OnPoseDetectionOutput(object stream, OutputStream<Detection>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;  // ��ȡ���ݰ�
        //    var value = packet == null ? default : packet.Get(Detection.Parser);  // ����ΪDetection����
        //    _poseDetectionAnnotationController.DrawLater(value);  // ���Ϊ�ӳٻ���
        //}

        // �沿�ؼ�����������
        private void OnFaceLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawFaceLandmarkListLater(value);  // �������沿�ؼ���
        }

        // �������ƹؼ�����������
        private void OnPoseLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawPoseLandmarkListLater(value);  // ���������ƹؼ���
        }

        // ���ֹؼ�����������
        private void OnLeftHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawLeftHandLandmarkListLater(value);  // ���������ֹؼ���
        }

        // ���ֹؼ�����������
        private void OnRightHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawRightHandLandmarkListLater(value);  // ���������ֹؼ���
        }

        // 3D�������ƹؼ�����������
        //private void OnPoseWorldLandmarksOutput(object stream, OutputStream<LandmarkList>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get(LandmarkList.Parser);
        //    _poseWorldLandmarksAnnotationController.DrawLater(value);  // 3D�ռ����
        //}

        // �ָ�������������
        //private void OnSegmentationMaskOutput(object stream, OutputStream<ImageFrame>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get();  // ֱ�ӻ�ȡImageFrame
        //    _segmentationMaskAnnotationController.DrawLater(value);  // ���Ʒָ�����
        //    value?.Dispose();  // �����ͷ���Դ����Ҫ�������ڴ�й©��
        //}

        // ����ROI������������
        //private void OnPoseRoiOutput(object stream, OutputStream<NormalizedRect>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get(NormalizedRect.Parser);
        //    _poseRoiAnnotationController.DrawLater(value);  // ���Ƹ���Ȥ����
        //}
    }
}