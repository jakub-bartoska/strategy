using component.strategy.minor_objects;
using Unity.Entities;

namespace component.strategy.general
{
    public struct MinorIdHolder : IComponentData
    {
        public long id;
        public MinorObjectType type;
    }
}