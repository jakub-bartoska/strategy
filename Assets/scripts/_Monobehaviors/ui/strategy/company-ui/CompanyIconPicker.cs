using component.config.game_settings;
using UnityEngine;
using UnityEngine.UI;

namespace _Monobehaviors.ui
{
    public class CompanyIconPicker : MonoBehaviour
    {
        [SerializeField] private RawImage image;

        public void setIcon(SoldierType type)
        {
            var icon = MonoBehaviourPrefabHolder.instance.getIconByType(type);
            image.texture = icon;
        }
    }
}