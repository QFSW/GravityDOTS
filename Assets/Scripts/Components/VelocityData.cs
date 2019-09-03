using Unity.Entities;
using Unity.Mathematics;

namespace QFSW.GravityDOTS
{
    public struct VelocityData : IComponentData
    {
        public float2 Velocity;
    }
}
