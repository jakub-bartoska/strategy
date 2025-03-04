﻿using System;
using Unity.Entities;

namespace component.strategy.buildings.building_costs
{
    public struct BuildingCostTag : IComponentData, IEquatable<BuildingCostTag>
    {
        public BuildingType buildingType;

        public bool Equals(BuildingCostTag other)
        {
            return (int) buildingType == (int) other.buildingType;
        }

        public override int GetHashCode()
        {
            return ((int) buildingType).GetHashCode();
        }
    }
}