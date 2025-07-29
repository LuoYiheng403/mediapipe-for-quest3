// Copyright (c) 2021 homuler
// 使用MIT许可证，详细条款可在LICENSE文件或https://opensource.org/licenses/MIT查看

using PassthroughCameraSamples;  // 用于XR直通相机功能的命名空间
using System.Collections;          // 提供IEnumerator等基础集合类
using UnityEngine;                 // Unity引擎核心命名空间
using UnityEngine.Rendering;       // 提供AsyncGPUReadback等渲染相关功能

namespace Mediapipe.Unity.Sample.Holistic
{
    // 全身追踪解决方案，继承自LegacySolutionRunner基类（指定使用HolisticTrackingGraph）
    public class HolisticTrackingSolution : LegacySolutionRunner<HolisticTrackingGraph>
    {
        // ========== 序列化字段（在Unity编辑器中可配置）==========
        [SerializeField] private CustomRenderWorldSpace _customRenderWorldSpace;
        //[SerializeField] private WebCamTextureManager _webCamTextureManager;  // 网络摄像头纹理管理器
        //[SerializeField] private RectTransform _worldAnnotationArea;          // 3D空间标注区域的矩形变换组件
        //[SerializeField] private DetectionAnnotationController _poseDetectionAnnotationController;       // 姿势检测标注控制器
        //[SerializeField] private HolisticLandmarkListAnnotationController _holisticAnnotationController; // 全身关键点标注控制器
        //[SerializeField] private PoseWorldLandmarkListAnnotationController _poseWorldLandmarksAnnotationController; // 3D姿势关键点标注控制器
        //[SerializeField] private MaskAnnotationController _segmentationMaskAnnotationController;        // 分割掩码标注控制器
        //[SerializeField] private NormalizedRectAnnotationController _poseRoiAnnotationController;       // 姿势ROI区域标注控制器

        // 纹理帧池（用于高效管理纹理内存）
        private Experimental.TextureFramePool _textureFramePool;

        // ========== 模型配置属性（代理到graphRunner）==========

        // 模型复杂度配置
        public HolisticTrackingGraph.ModelComplexity modelComplexity
        {
            get => graphRunner.modelComplexity;  // 获取当前模型复杂度
            set => graphRunner.modelComplexity = value;  // 设置模型复杂度（Lite/Full/Heavy）
        }

        // 是否平滑关键点
        public bool smoothLandmarks
        {
            get => graphRunner.smoothLandmarks;
            set => graphRunner.smoothLandmarks = value;
        }

        // 是否优化面部关键点
        public bool refineFaceLandmarks
        {
            get => graphRunner.refineFaceLandmarks;
            set => graphRunner.refineFaceLandmarks = value;
        }

        // 是否启用分割
        public bool enableSegmentation
        {
            get => graphRunner.enableSegmentation;
            set => graphRunner.enableSegmentation = value;
        }

        // 是否平滑分割结果
        public bool smoothSegmentation
        {
            get => graphRunner.smoothSegmentation;
            set => graphRunner.smoothSegmentation = value;
        }

        // 最小检测置信度阈值
        public float minDetectionConfidence
        {
            get => graphRunner.minDetectionConfidence;
            set => graphRunner.minDetectionConfidence = value;
        }

        // 最小跟踪置信度阈值
        public float minTrackingConfidence
        {
            get => graphRunner.minTrackingConfidence;
            set => graphRunner.minTrackingConfidence = value;
        }

        // ========== 生命周期方法 ==========

        // 停止解决方案（重写基类方法）
        public override void Stop()
        {
            base.Stop();  // 调用基类停止逻辑
            _textureFramePool?.Dispose();  // 安全释放纹理帧池资源
            _textureFramePool = null;      // 清空引用便于垃圾回收
        }

        // ========== 核心协程：解决方案主循环 ==========
        protected override IEnumerator Run()
        {
            // 初始化MediaPipe图并等待完成（异步操作）
            var graphInitRequest = graphRunner.WaitForInit(runningMode);

            // 从ImageSourceProvider获取图像源实例
            var imageSource = ImageSourceProvider.ImageSource;

            // 启动图像源播放（异步操作）
            yield return imageSource.Play();

            // 检查图像源是否准备就绪
            if (!imageSource.isPrepared)
            {
                Debug.LogError("图像源启动失败，退出...");
                yield break;  // 提前终止协程
            }

            // 创建纹理帧池（RGBA32格式）
            // 参数：纹理宽度、高度、格式、池容量
            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth,
                imageSource.textureHeight,
                TextureFormat.RGBA32,
                10  // 预分配10个纹理帧
            );

            // 初始化UI屏幕组件（根据图像源调整尺寸）
            //screen.Initialize(imageSource);

            // 调整3D标注区域旋转（补偿图像源旋转）
            // Reverse()将图像源旋转转换为世界空间旋转
            //_worldAnnotationArea.localEulerAngles = imageSource.rotation.Reverse().GetEulerAngles();

            // 等待图初始化完成（yield直到异步操作完成）
            yield return graphInitRequest;

            // 检查初始化是否出错
            if (graphInitRequest.isError)
            {
                Debug.LogError(graphInitRequest.error);
                yield break;
            }

            // ===== 异步模式设置 =====
            if (!runningMode.IsSynchronous())  // 如果是异步运行模式
            {
                // 注册所有输出流的事件处理器
                //graphRunner.OnPoseDetectionOutput += OnPoseDetectionOutput;
                graphRunner.OnFaceLandmarksOutput += OnFaceLandmarksOutput;
                graphRunner.OnPoseLandmarksOutput += OnPoseLandmarksOutput;
                graphRunner.OnLeftHandLandmarksOutput += OnLeftHandLandmarksOutput;
                graphRunner.OnRightHandLandmarksOutput += OnRightHandLandmarksOutput;
                //graphRunner.OnPoseWorldLandmarksOutput += OnPoseWorldLandmarksOutput;
                //graphRunner.OnSegmentationMaskOutput += OnSegmentationMaskOutput;
                //graphRunner.OnPoseRoiOutput += OnPoseRoiOutput;
            }

            // ===== 初始化标注控制器 =====
            // 为每个控制器设置图像源参考
            //SetupAnnotationController(_poseDetectionAnnotationController, imageSource);
            //SetupAnnotationController(_holisticAnnotationController, imageSource);
            //SetupAnnotationController(_poseWorldLandmarksAnnotationController, imageSource);
            //SetupAnnotationController(_segmentationMaskAnnotationController, imageSource);

            // 特别初始化分割掩码控制器（需要知道纹理尺寸）
            //_segmentationMaskAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            //SetupAnnotationController(_poseRoiAnnotationController, imageSource);

            // 启动MediaPipe图运行（传入图像源配置）
            graphRunner.StartRun(imageSource);

            // ===== 准备GPU异步读取 =====
            AsyncGPUReadbackRequest req = default;  // GPU读取请求结构体
            var waitUntilReqDone = new WaitUntil(() => req.done);  // 等待条件：请求完成

            // 检查是否支持GPU加速（仅Android+OpenGLES且GPU资源可用）
            var canUseGpuImage = graphRunner.configType == GraphRunner.ConfigType.OpenGLES &&
                               GpuManager.GpuResources != null;

            // 获取OpenGL上下文（如果支持GPU加速）
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            // ===== 主处理循环 =====
            while (true)  // 无限循环直到被外部停止
            {
                // 暂停状态处理
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);  // 等待直到取消暂停
                }

                // 从池中获取可用纹理帧
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();  // 等待帧结束
                    continue;  // 跳过当前迭代
                }

                // === 将当前图像复制到纹理帧 ===
                if (canUseGpuImage)  // GPU加速路径
                {
                    // 等待当前帧渲染完成
                    yield return new WaitForEndOfFrame();

                    // 直接从GPU读取纹理（避免CPU复制）
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture());
                }
                else  // CPU路径
                {
                    // 发起异步GPU读取请求
                    // 参数：源纹理、mipmap级别、垂直翻转
                    req = textureFrame.ReadTextureAsync(
                        imageSource.GetCurrentTexture(),
                        false,
                        imageSource.isVerticallyFlipped
                    );

                    // 等待读取完成
                    yield return waitUntilReqDone;

                    // 检查读取错误
                    if (req.hasError)
                    {
                        Debug.LogWarning("从图像源读取纹理失败");
                        yield return new WaitForEndOfFrame();
                        continue;
                    }
                }

                // 将纹理帧送入MediaPipe输入流
                // glContext在支持GPU时提供共享上下文
                graphRunner.AddTextureFrameToInputStream(textureFrame, glContext);

                // === 同步处理模式 ===
                if (runningMode.IsSynchronous())
                {
                    // 更新屏幕显示（使用当前纹理帧）
                    //screen.ReadSync(textureFrame);

                    // 等待下一批处理结果（异步任务）
                    var task = graphRunner.WaitNextAsync();
                    yield return new WaitUntil(() => task.IsCompleted);

                    // 获取结果
                    var result = task.Result;

                    // 立即绘制所有标注
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

                    // 释放分割掩码资源（如果存在）
                    result.segmentationMask?.Dispose();
                }
            }  // 结束while循环
        }  // 结束Run协程

        // ========== 异步模式事件处理器 ==========

        // 姿势检测结果处理器
        //private void OnPoseDetectionOutput(object stream, OutputStream<Detection>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;  // 获取数据包
        //    var value = packet == null ? default : packet.Get(Detection.Parser);  // 解析为Detection对象
        //    _poseDetectionAnnotationController.DrawLater(value);  // 标记为延迟绘制
        //}

        // 面部关键点结果处理器
        private void OnFaceLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawFaceLandmarkListLater(value);  // 仅更新面部关键点
        }

        // 身体姿势关键点结果处理器
        private void OnPoseLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawPoseLandmarkListLater(value);  // 仅更新姿势关键点
        }

        // 左手关键点结果处理器
        private void OnLeftHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawLeftHandLandmarkListLater(value);  // 仅更新左手关键点
        }

        // 右手关键点结果处理器
        private void OnRightHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            //_holisticAnnotationController.DrawRightHandLandmarkListLater(value);  // 仅更新右手关键点
        }

        // 3D世界姿势关键点结果处理器
        //private void OnPoseWorldLandmarksOutput(object stream, OutputStream<LandmarkList>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get(LandmarkList.Parser);
        //    _poseWorldLandmarksAnnotationController.DrawLater(value);  // 3D空间绘制
        //}

        // 分割掩码结果处理器
        //private void OnSegmentationMaskOutput(object stream, OutputStream<ImageFrame>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get();  // 直接获取ImageFrame
        //    _segmentationMaskAnnotationController.DrawLater(value);  // 绘制分割掩码
        //    value?.Dispose();  // 立即释放资源（重要！避免内存泄漏）
        //}

        // 姿势ROI区域结果处理器
        //private void OnPoseRoiOutput(object stream, OutputStream<NormalizedRect>.OutputEventArgs eventArgs)
        //{
        //    var packet = eventArgs.packet;
        //    var value = packet == null ? default : packet.Get(NormalizedRect.Parser);
        //    _poseRoiAnnotationController.DrawLater(value);  // 绘制感兴趣区域
        //}
    }
}