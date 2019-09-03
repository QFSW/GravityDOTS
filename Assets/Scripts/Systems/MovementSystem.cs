using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    public class MovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            Entities.ForEach((ref Velocity velocity, ref Translation translation) =>
            {
                float2 dx = velocity.Value * dt;
                translation.Value += new float3(dx, 0);
            });
        }
    }
}
