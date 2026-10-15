using Spine.Unity;
using TMPro;
using UnityEngine;

namespace CashRewards.ComboEffects
{
    /// <summary>Static UGUI prefab. Original 1.0.23 art, combo thresholds and number animation.</summary>
    public sealed class ComboEffectPlayer : MonoBehaviour
    {
        public enum Word { Combo, Perfect, Excellent }
        public enum Emoji { Smile, StarEyes, Applause, Kiss, Clap, Surprise, Sunglasses }

        [SerializeField] SkeletonGraphic[] words = new SkeletonGraphic[3];
        [SerializeField] SkeletonGraphic[] emojis = new SkeletonGraphic[7];
        [SerializeField] TextMeshProUGUI comboNumber;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip perfectSound;
        [SerializeField] AudioClip excellentSound;
        [SerializeField] bool playSound = true;
        [SerializeField, Min(0)] float riseDistance = 14.4f;
        [SerializeField, Min(0)] float numberGap = 14.4f;
        [SerializeField] float comboArtScale = 240f / 191f;

        SkeletonGraphic activeWord, activeEmoji;
        Vector2 wordOrigin, numberOrigin;
        Vector3 numberScale;
        float elapsed, wordDuration, emojiElapsed, emojiDuration, numberWidth;
        int wordIndex = -1;

        public bool IsPlaying => activeWord != null || activeEmoji != null;

        void Awake()
        {
            numberOrigin = comboNumber.rectTransform.anchoredPosition;
            numberScale = comboNumber.rectTransform.localScale;
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
            comboNumber.gameObject.SetActive(wordIndex == 0);
            comboNumber.text = "×" + count;
            numberWidth = comboNumber.GetPreferredValues().x + 4;
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
            float y = riseDistance * Mathf.SmoothStep(0, 1, elapsed / wordDuration);
            float x = 0;
            if (wordIndex == 0)
            {
                float scale = elapsed < .083f ? 1 + Mathf.Clamp01(elapsed / .083f)
                    : elapsed < .2f ? Mathf.Lerp(2, .9f, (elapsed - .083f) / .117f)
                    : Mathf.Lerp(.9f, 1, Mathf.Clamp01((elapsed - .2f) / .133f));
                comboNumber.rectTransform.localScale = numberScale * scale;
                comboNumber.alpha = Mathf.Clamp01((elapsed - .05f) / .033f) * Mathf.Clamp01((wordDuration - elapsed) / .2f);
                x = -.5f * (scale * numberWidth + numberGap);
                float width = 191 * activeWord.Skeleton.FindBone("bone2").ScaleX * comboArtScale;
                comboNumber.rectTransform.anchoredPosition = numberOrigin + new Vector2(x + width * .5f + numberGap, y);
            }
            activeWord.rectTransform.anchoredPosition = wordOrigin + new Vector2(x, y);
        }

        public void Hide()
        {
            HideWord(); HideEmoji();
            foreach (var g in words) if (g) g.gameObject.SetActive(false);
            foreach (var g in emojis) if (g) g.gameObject.SetActive(false);
            if (comboNumber) comboNumber.gameObject.SetActive(false);
        }
        void HideWord()
        {
            if (activeWord)
            {
                activeWord.rectTransform.anchoredPosition = wordOrigin;
                activeWord.gameObject.SetActive(false);
            }
            activeWord = null;
            if (comboNumber) { comboNumber.rectTransform.anchoredPosition = numberOrigin; comboNumber.rectTransform.localScale = numberScale; comboNumber.gameObject.SetActive(false); }
        }
        void HideEmoji() { if (activeEmoji) activeEmoji.gameObject.SetActive(false); activeEmoji = null; }
        void OnDisable() { Hide(); }
    }
}
