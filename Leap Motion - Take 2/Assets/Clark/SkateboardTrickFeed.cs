using UnityEngine;
using UnityEngine.UI;

namespace Fingerboard
{
    public class SkateboardTrickFeed : MonoBehaviour
    {
        [Header("Image Slots")]
        [SerializeField] private Image firstSlot;
        [SerializeField] private Image secondSlot;
        [SerializeField] private Image thirdSlot;

        [Header("Trick Sprites")]
        [SerializeField] private Sprite kickflip;
        [SerializeField] private Sprite grinding;
        [SerializeField] private Sprite ollie;
        [SerializeField] private Sprite popShoveIt;

        private Sprite latest;
        private Sprite previous;
        private Sprite oldest;

        private void Awake() => Refresh();

        public void ShowKickflip() => Push(kickflip);
        public void ShowGrinding() => Push(grinding);
        public void ShowOllie() => Push(ollie);
        public void ShowPopShoveIt() => Push(popShoveIt);

        public void Clear()
        {
            latest = null;
            previous = null;
            oldest = null;
            Refresh();
        }

        private void Push(Sprite sprite)
        {
            if (sprite == null) return;

            oldest = previous;
            previous = latest;
            latest = sprite;
            Refresh();
        }

        private void Refresh()
        {
            SetSlot(firstSlot, latest);
            SetSlot(secondSlot, previous);
            SetSlot(thirdSlot, oldest);
        }

        private static void SetSlot(Image slot, Sprite sprite)
        {
            if (slot == null) return;

            slot.sprite = sprite;
            slot.preserveAspect = true;
            slot.enabled = sprite != null;
        }
    }
}
