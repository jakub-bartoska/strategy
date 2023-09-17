using UnityEngine;

namespace Unity.Physics.Authoring
{
    sealed class EnumFlagsAttribute : PropertyAttribute
    {
    }

    sealed class ExpandChildrenAttribute : PropertyAttribute
    {
    }

    sealed class SoftRangeAttribute : PropertyAttribute
    {
        public readonly float SliderMax;
        public readonly float SliderMin;

        public SoftRangeAttribute(float min, float max)
        {
            SliderMin = TextFieldMin = min;
            SliderMax = TextFieldMax = max;
        }

        public float TextFieldMin { get; set; }
        public float TextFieldMax { get; set; }
    }
}