using component._common.system_switchers;
using Unity.Entities;
using UnityEngine.InputSystem;
using utils;

namespace component._common.controlls
{
    public partial class EscapeKeySystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<SystemStatusHolder>();
            var inputs = InputUtils.getInputs();
            inputs.common.escape.started += onEscape;
        }

        private void onEscape(InputAction.CallbackContext ctx)
        {
            var blockers = SystemAPI.GetSingletonBuffer<SystemSwitchBlocker>();
            blockers.Add(new SystemSwitchBlocker
            {
                blocker = Blocker.AUTO_ADD_BLOCKERS
            });

            var systemholder = SystemAPI.GetSingletonRW<SystemStatusHolder>();
            if (systemholder.ValueRO.currentStatus == SystemStatus.INGAME_MENU)
            {
                systemholder.ValueRW.desiredStatus = systemholder.ValueRO.previousStatus;
            }
            else
            {
                systemholder.ValueRW.desiredStatus = SystemStatus.INGAME_MENU;
            }
        }

        protected override void OnUpdate()
        {
        }
    }
}