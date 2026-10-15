using UnityEngine;

namespace CashRewards.ComboEffects
{
    public sealed class ComboEffectDemo : MonoBehaviour
    {
        [SerializeField] ComboEffectPlayer player;
        [SerializeField] bool autoPlay = true;
        [SerializeField] float interval = 3f;
        readonly int[] counts = { 3, 5, 10, 15, 20, 30, 40, 100 };
        int index;
        float next;
        void Update()
        {
            if (!autoPlay || Time.unscaledTime < next) return;
            player.PlayCombo(counts[index++ % counts.Length]);
            next = Time.unscaledTime + Mathf.Max(1,interval);
        }
        public void Play(int count) { autoPlay = false; player.PlayCombo(count); }
        public void Resume() { autoPlay = true; next = 0; }
    }
}
