using System;
using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ParticleEffects
{
    // The RawImage, camera and ParticleSystem are static prefab children.
    // This adapter only supplies their render target, viewport fit and playback.
    [DisallowMultipleComponent]
    public sealed class CashParticleUIPlayer : MonoBehaviour
    {
        [SerializeField] private Camera particleCamera;
        [SerializeField] private RawImage display;
        [SerializeField] private Transform simulationRoot;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private bool playOnEnable=true;
        [SerializeField] private int renderTextureHeight=960;
        [SerializeField] private Vector2 designSize=new Vector2(1080,1920);
        private RenderTexture target;
        private ParticleSystem[] systems;
        private ParticleSystem[] Systems => systems ?? (simulationRoot ? systems=simulationRoot.GetComponentsInChildren<ParticleSystem>(true) : Array.Empty<ParticleSystem>());
        private bool playing;
        public bool IsPlaying=>playing;
        public int AliveParticles { get { int count=0;foreach(var system in Systems)count+=system.particleCount;return count; } }
        public event Action Completed;

        private void OnEnable(){if(Application.isPlaying && playOnEnable)PlayOnce();}

        public void PlayOnce()
        {
            Prepare();
            foreach(var system in Systems)system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            display.enabled=true;particleCamera.enabled=true;
            foreach(var system in Systems)system.Play(true);playing=true;
        }

        public void Hide()
        {
            playing=false;
            foreach(var system in Systems)system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(display)display.enabled=false;
            if(particleCamera)particleCamera.enabled=false;
        }

        private void Update()
        {
            if(!playing)return;
            foreach(var system in Systems)if(system.IsAlive(true))return;
            Hide();Completed?.Invoke();
        }

        private void OnRectTransformDimensionsChange(){if(target)Prepare();}
        private void Prepare()
        {
            if(!particles || !particleCamera || !display || !simulationRoot)
                throw new MissingReferenceException("Use the complete CashRainParticlesUI prefab.");
            var rect=((RectTransform)transform).rect;
            float w=Mathf.Max(1,rect.width),h=Mathf.Max(1,rect.height);
            float scale=Mathf.Max(w/designSize.x,h/designSize.y);
            simulationRoot.localScale=Vector3.one*scale;
            particleCamera.orthographicSize=h*.5f*Mathf.Abs(transform.lossyScale.y);
            particleCamera.nearClipPlane=.1f;
            particleCamera.farClipPlane=Mathf.Max(10,6000*Mathf.Abs(transform.lossyScale.z));
            int height=Mathf.Clamp(renderTextureHeight,128,2048),width=Mathf.Clamp(Mathf.RoundToInt(height*w/h),64,4096);
            if(!target || target.width!=width || target.height!=height)
            {
                ReleaseTarget();
                target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32) { name="CashRainParticlesUI RenderTarget" };
                target.Create();particleCamera.targetTexture=target;display.texture=target;
            }
        }
        private void OnDisable(){Hide();}
        private void OnDestroy(){ReleaseTarget();}
        private void ReleaseTarget()
        {
            if(particleCamera)particleCamera.targetTexture=null;
            if(display)display.texture=null;
            if(!target)return;
            target.Release();if(Application.isPlaying)Destroy(target);else DestroyImmediate(target);target=null;
        }
    }
}
