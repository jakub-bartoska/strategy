using System;
using TMPro;
using UnityEngine;

namespace _Monobehaviors.debug
{
    public class FpsMonobehavior : MonoBehaviour
    {
        public static FpsMonobehavior instance;

        [SerializeField] private TextMeshProUGUI text;

        private void Awake()
        {
            instance = this;
        }

        public void updateFps(int fps)
        {
            text.text = fps.ToString();
        }
    }
}