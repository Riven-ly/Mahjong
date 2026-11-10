using Spine.Unity;
using TMPro;
using UnityEngine;

namespace CashRewards.ComboEffects
{
    /// <summary>Plays the combo label, reward words and emojis.</summary>
    public sealed class ComboEffectPlayer : MonoBehaviour
    {
        public enum Word { Combo, Perfect, Excellent }
        public enum Emoji { Smile, StarEyes, Applause, Kiss, Clap, Surprise, Sunglasses }

        [SerializeField] SkeletonGraphic[] words = new SkeletonGraphic[3];
        [SerializeField] SkeletonGraphic[] emojis = new SkeletonGraphic[7];
        [SerializeField] TextMeshProUGUI comboNumber;
        [SerializeField] TextMeshProUGUI comboOutline;
        [SerializeField] TextMeshProUGUI comboShadow;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip perfectSound;
        [SerializeField] AudioClip excellentSound;
        [SerializeField] bool playSound = true;
        [SerializeField, Min(0)] float riseDistance = 14.4f;

        SkeletonGraphic activeWord, activeEmoji;
        Spine.Slot comboAnimationSlot;
        Vector2 wordOrigin, comboOrigin, shadowOffset;
        Vector3 comboScale;
        float elapsed, wordDuration, emojiElapsed, emojiDuration;
        int wordIndex = -1;

        public bool IsPlaying => activeWord != null || activeEmoji != null;

        void Awake()
        {
            comboOrigin = comboNumber.rectTransform.anchoredPosition;
            comboScale = comboNumber.rectTransform.localScale;
            shadowOffset = comboShadow.rectTransform.anchoredPosition - comboOrigin;
            foreach (var g in words) Configure(g);
            foreach (var g in emojis) Configure(g);
            Hide();
        }
        static void Configure(SkeletonGraphic g)
        {
            if (!g) return;
            g.UpdateTiming = UpdateTiming.ManualUpdate;
            g.raycastTarget = false;
        }

        public void PlayCombo(int count)
        {
            Hide();
            if (count < 3) return;
            int word = count == 5 || count == 10 ? 1 :
                count == 15 || count == 20 || count >= 30 && count % 10 == 0 ? 2 : 0;
            PlayWord((Word)word, count);
            int emoji = count == 5 ? 0 : count == 10 ? 1 : count == 15 ? 2 : count == 20 ? 3 :
                count < 30 || count % 10 != 0 ? -1 : count >= 100 ? 6 : count % 20 == 10 ? 4 : 5;
            if (emoji >= 0) PlayEmoji((Emoji)emoji);
        }

        // Separate calls support rewards unrelated to the original combo thresholds.
        public void PlayWord(Word word, int count = 3)
        {
            HideWord();
            wordIndex = (int)word;
            activeWord = words[wordIndex];
            if (!activeWord) return;
            wordOrigin = activeWord.rectTransform.anchoredPosition;
            elapsed = 0;
            wordDuration = Restart(activeWord);
            if (word == Word.Combo)
            {
                // The Spine slot supplies the scale and fade for all text layers.
                comboAnimationSlot = activeWord.Skeleton.FindSlot("mj_combo");
                string label = $"Combo <space=24><size=132%>{count}</size>";
                comboNumber.text = comboOutline.text = comboShadow.text = label;
                comboNumber.gameObject.SetActive(true);
                comboOutline.gameObject.SetActive(true);
                comboShadow.gameObject.SetActive(true);
            }
            if (playSound && audioSource && wordIndex > 0)
            {
                var clip = wordIndex == 1 ? perfectSound : excellentSound;
                if (clip)
                {
                    float volume = wordIndex == 1f ? 0.5f : 1f;
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.SetAudioSource(audioSource, clip.name, volume);
                    else
                        audioSource.PlayOneShot(clip, volume);
                }
            }
            ApplyWordLayout();
        }

        public void PlayEmoji(Emoji emoji)
        {
            HideEmoji();
            activeEmoji = emojis[(int)emoji];
            if (!activeEmoji) return;
            emojiElapsed = 0;
            emojiDuration = Restart(activeEmoji);
        }

        static float Restart(SkeletonGraphic graphic)
        {
            graphic.gameObject.SetActive(true);
            graphic.Initialize(false);
            graphic.Skeleton.SetToSetupPose();
            graphic.AnimationState.ClearTracks();
            var track = graphic.AnimationState.SetAnimation(0, "animation", false);
            graphic.Update(0);
            return track.Animation.Duration;
        }

        void Update()
        {
            if (activeWord)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, wordDuration - elapsed);
                elapsed += dt;
                activeWord.Update(dt);
                ApplyWordLayout();
                if (elapsed >= wordDuration) HideWord();
            }
            if (activeEmoji)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, emojiDuration - emojiElapsed);
                emojiElapsed += dt;
                activeEmoji.Update(dt);
                if (emojiElapsed >= emojiDuration) HideEmoji();
            }
        }

        void ApplyWordLayout()
        {
            if (!activeWord) return;
            var rise = Vector2.up * (riseDistance * Mathf.SmoothStep(0, 1, elapsed / wordDuration));
            activeWord.rectTransform.anchoredPosition = wordOrigin + rise;
            if (comboAnimationSlot != null)
            {
                var bone = comboAnimationSlot.Bone;
                var scale = new Vector3(Mathf.Max(.001f, bone.ScaleX), Mathf.Max(.001f, bone.ScaleY), 1);
                var position = comboOrigin + rise;
                var textScale = Vector3.Scale(comboScale, scale);
                comboNumber.rectTransform.localScale = comboOutline.rectTransform.localScale = textScale;
                comboShadow.rectTransform.localScale = textScale;
                comboNumber.rectTransform.anchoredPosition = comboOutline.rectTransform.anchoredPosition = position;
                comboShadow.rectTransform.anchoredPosition = position + Vector2.Scale(shadowOffset, scale);
                comboNumber.alpha = comboOutline.alpha = comboShadow.alpha = comboAnimationSlot.A;
            }
        }

        public void Hide()
        {
            HideWord(); HideEmoji();
            foreach (var g in words) if (g) g.gameObject.SetActive(false);
            foreach (var g in emojis) if (g) g.gameObject.SetActive(false);
        }
        void HideWord()
        {
            if (activeWord)
            {
                activeWord.rectTransform.anchoredPosition = wordOrigin;
                activeWord.gameObject.SetActive(false);
            }
            activeWord = null;
            comboAnimationSlot = null;
            if (comboNumber) comboNumber.gameObject.SetActive(false);
            if (comboOutline) comboOutline.gameObject.SetActive(false);
            if (comboShadow) comboShadow.gameObject.SetActive(false);
        }
        void HideEmoji() { if (activeEmoji) activeEmoji.gameObject.SetActive(false); activeEmoji = null; }
        void OnDisable() { Hide(); }
    }
}
