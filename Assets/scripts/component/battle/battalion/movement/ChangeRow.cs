﻿using system.battle.enums;
using Unity.Entities;

namespace component.battle.battalion.markers
{
    public struct ChangeRow : IComponentData
    {
        public Direction direction;
        public ChangeState state;
        public Entity shadowEntity;
    }

    public enum ChangeState
    {
        INIT,
        RUNNING
    }
}