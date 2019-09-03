using Unity.Entities;
using Unity.Mathematics;

namespace QFSW.GravityDOTS
{
    public struct Velocity : IComponentData
    {
        public float2 Value;
    }
}
