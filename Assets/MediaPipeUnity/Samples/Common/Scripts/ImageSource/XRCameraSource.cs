using PassthroughCameraSamples;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
using UnityEngine.Video;
#endif


namespace Mediapipe.Unity
{
    public class XRCameraSource : ImageSource
    {
        public WebCamTextureManager _webCamTextureManager=GameObject.Find("WebCamTextureManagerPrefab").GetComponent<WebCamTextureManager>();
        private WebCamTexture _WebCamTexture;
        private WebCamTexture WebCamTexture
        {
            get => _WebCamTexture;
            set
            {
                _WebCamTexture= value;
            }
        }

        private readonly ResolutionStruct[] _defaultAvailableResolutions;

        //public XRCameraSource()

        public override int textureWidth => !isPrepared ? 0 : WebCamTexture.width;
        public override int textureHeight => !isPrepared ? 0 : WebCamTexture.height;


        public override string sourceName => WebCamTexture.name;

        public override string[] sourceCandidateNames => throw new NotImplementedException();

        public override ResolutionStruct[] availableResolutions
        {
            get
            {
                // 对于 XR 设备，返回预设的推荐分辨率
                return new[]
                {
                    new ResolutionStruct(1280, 960, 60),
                    new ResolutionStruct(800, 600, 60),
                    new ResolutionStruct(640, 480, 60),
                    new ResolutionStruct(320, 240, 60)

                };
            }
        }

        public override bool isPrepared => WebCamTexture != null;

        public override bool isPlaying => WebCamTexture != null && WebCamTexture.isPlaying;

        public override bool isVerticallyFlipped => isPrepared && WebCamTexture.videoVerticallyMirrored;

        public override bool isFrontFacing => true;

        public override RotationAngle rotation => isPrepared ? (RotationAngle)WebCamTexture.videoRotationAngle : RotationAngle.Rotation0;

        public override Texture GetCurrentTexture() => WebCamTexture != null ? WebCamTexture: null;



        public override void Pause()
        {
            if (isPlaying)
            {
                WebCamTexture.Pause();
            }
        }

        public override IEnumerator Play()
        {
            while (_webCamTextureManager.WebCamTexture == null)
            {
                yield return null;
            }
            WebCamTexture = _webCamTextureManager.WebCamTexture;
        }

        public override IEnumerator Resume()
        {
            if (!isPrepared)
            {
                throw new InvalidOperationException("摄像头尚未准备就绪，无法恢复播放");
            }

            if (!isPlaying)
            {
                WebCamTexture.Play();
            }

            yield return new WaitUntil(() => isPlaying);
        }

        public override void SelectSource(int sourceId)
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            if (isPlaying)
            {
                WebCamTexture.Stop();
            }
            WebCamTexture = null;
        }
    }
}