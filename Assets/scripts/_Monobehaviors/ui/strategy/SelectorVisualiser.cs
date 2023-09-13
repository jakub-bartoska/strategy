using UnityEngine;

namespace _Monobehaviors
{
    public class SelectorVisualiser : MonoBehaviour
    {
        public static SelectorVisualiser instance;

        public RectTransform rectTransform;
        public GameObject image;

        public void Awake()
        {
            instance = this;
        }
    }
}