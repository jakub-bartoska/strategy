using Unity.Entities;

namespace component.formation
{
    public struct SoldierFormationStatus : IComponentData
    {
        public int formationId;
        public FormationStatus formationStatus;
    }

    public enum FormationStatus
    {
        NO_FORMATION,
        IN_FORMATION
    }
}