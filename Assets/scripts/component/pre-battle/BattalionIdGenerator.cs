﻿using Unity.Entities;

namespace component.pre_battle
{
    public struct BattalionIdGenerator : IComponentData
    {
        public long nextBattalionIdToBeUsed;
    }
}