using Unity.Collections;
using Unity.Entities;

namespace component.battle.battalion.data_holders
{
    public struct SoldierPositions : IComponentData
    {
        /**
         * soldier conunt - ordered soldier positions
         *
         * example: 2 = 1, 10
         * this means that if battalion has 2 soldiers, they should be placed on positions 1 and 10 within their battalion
         */
        public NativeHashMap<int, NativeList<int>> positions;
    }
}