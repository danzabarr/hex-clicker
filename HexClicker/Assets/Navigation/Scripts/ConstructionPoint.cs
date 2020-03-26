using UnityEngine;

namespace HexClicker.Navigation
{
    public class ConstructionPoint : WorkPoint
    {
        [SerializeField] private float maxProgress;
        private float progress;
        public float Progress
        {
            get => progress;
            set => progress = Mathf.Clamp(value, 0, maxProgress);
        }

        public float MaxProgress => maxProgress;
        public float FractionComplete
        {
            get => Mathf.Clamp(Progress / maxProgress, 0, 1);
            set => Progress = Mathf.Clamp(value * maxProgress, 0, 1);
        }

        public float FractionCompleteUnclamped
        {
            get => Progress / maxProgress;
            set => Progress = value * maxProgress;
        }

        public void Complete() => Progress = maxProgress;
        public void Restart() => Progress = 0;
        public bool IsComplete => FractionComplete >= 1;
    }
}
