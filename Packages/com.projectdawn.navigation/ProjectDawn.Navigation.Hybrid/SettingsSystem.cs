using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SettingsSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            foreach (var type in SettingsBehaviour.Types)
            {
                var settings = GameObject.FindAnyObjectByType(type) as SettingsBehaviour;
                if (settings != null)
                {
                    settings.GetOrCreateEntity();
                }
            }
        }
    }
}