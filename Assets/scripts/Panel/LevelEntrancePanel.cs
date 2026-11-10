using System;
using Spine.Unity;
using UnityEngine;

public sealed class LevelEntrancePanel : UIBase
{
    public sealed class CloseThenOpen
    {
        public Action OnClosed { get; }
        public Action OnOpened { get; }

        public CloseThenOpen(Action onClosed, Action onOpened)
        {
            OnClosed = onClosed;
            OnOpened = onOpened;
        }
    }

    private const string CloseAnimation = "Appear";
    private const string OpenAnimation = "Disappear";
    // Closed-pose art bounds at the game's default canvas pixels per unit.
    private static readonly Vector2 ClosedDoorSize = new Vector2(1080f, 2780f);

    [SerializeField] private RectTransform doorArt;
    [SerializeField] private SkeletonGraphic door;
    [SerializeField] private SkeletonGraphic leaves;
    [SerializeField] private AudioSource doorAudio;

    private float timeRemaining;
    private double lastUpdateTime;
    private Action onAnimationComplete;

    public override void Refresh(object data = null)
    {
        isOpen = true;
        StopDoorAnimation();
        var transition = data as CloseThenOpen;
        Action onOpened = transition?.OnOpened ?? data as Action;
        var panel = (RectTransform)transform;
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.localScale = Vector3.one;

        Canvas.ForceUpdateCanvases();
        FitDoorToPanel();

        if (!CanPlayDoor())
        {
            Debug.LogError("The level entrance Spine animations are missing.", this);
            transition?.OnClosed?.Invoke();
            onOpened?.Invoke();
            return;
        }

        doorArt.gameObject.SetActive(true);
        void OpenDoor()
        {
            PlayAnimation(OpenAnimation, "open_door_sd", () =>
            {
                doorArt.gameObject.SetActive(false);
                onOpened?.Invoke();
            });
        }

        if (transition == null)
        {
            OpenDoor();
            return;
        }

        PlayAnimation(CloseAnimation, "close_door_sd", () =>
        {
            transition.OnClosed?.Invoke();
            if (isActiveAndEnabled && onAnimationComplete == null)
                OpenDoor();
        });
    }

    private bool CanPlayDoor()
    {
        if (doorArt == null || door == null || leaves == null)
            return false;

        door.Initialize(false);
        leaves.Initialize(false);
        return door.IsValid && leaves.IsValid
            && door.Skeleton.Data.FindAnimation(CloseAnimation) != null
            && door.Skeleton.Data.FindAnimation(OpenAnimation) != null
            && leaves.Skeleton.Data.FindAnimation(CloseAnimation) != null
            && leaves.Skeleton.Data.FindAnimation(OpenAnimation) != null;
    }

    private void PlayAnimation(string animationName, string soundName, Action onComplete)
    {
        timeRemaining = RestartAnimation(door, animationName);
        RestartAnimation(leaves, animationName);
        lastUpdateTime = Time.realtimeSinceStartupAsDouble;
        onAnimationComplete = onComplete;
        if (doorAudio != null && AudioManager.Instance != null)
            AudioManager.Instance.SetAudioSource(doorAudio, soundName);
    }

    private static float RestartAnimation(SkeletonGraphic graphic, string animationName)
    {
        graphic.AnimationState.ClearTracks();
        graphic.Skeleton.SetToSetupPose();
        var entry = graphic.AnimationState.SetAnimation(0, animationName, false);
        graphic.Update(0f);
        graphic.UpdateMesh();
        return entry.Animation.Duration;
    }

    private void Update()
    {
        if (onAnimationComplete == null)
            return;

        double now = Time.realtimeSinceStartupAsDouble;
        float deltaTime = Mathf.Min((float)(now - lastUpdateTime), timeRemaining);
        lastUpdateTime = now;
        timeRemaining -= deltaTime;
        door.Update(deltaTime);
        leaves.Update(deltaTime);
        if (timeRemaining > 0f)
            return;

        var completed = onAnimationComplete;
        onAnimationComplete = null;
        completed();
    }

    private void OnRectTransformDimensionsChange()
    {
        FitDoorToPanel();
    }

    private void FitDoorToPanel()
    {
        if (doorArt == null)
            return;

        Vector2 panelSize = ((RectTransform)transform).rect.size;
        float coverScale = Mathf.Max(panelSize.x / ClosedDoorSize.x, panelSize.y / ClosedDoorSize.y);
        doorArt.localScale = new Vector3(coverScale, coverScale, 1f);
    }

    private void OnDisable()
    {
        isOpen = false;
        StopDoorAnimation();
    }

    private void StopDoorAnimation()
    {
        onAnimationComplete = null;
        if (doorAudio != null)
            doorAudio.Stop();
        if (doorArt != null)
            doorArt.gameObject.SetActive(false);
    }
}
