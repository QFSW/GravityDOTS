using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class BoundingBoxSystem : JobComponentSystem
    {
        private struct BoundingJob : IJobForEach<Velocity, LocalToWorld, Radius, Bounded>
        {
            public float2 BoundsX;
            public float2 BoundsY;

            public void Execute(ref Velocity velocity, [ReadOnly] ref LocalToWorld transform,
                                [ReadOnly] ref Radius radius, [ReadOnly] ref Bounded bounded)
            {
                float2 pos = transform.Position.xy;
                float r = radius.Value;

                if (pos.y < BoundsY.x + r || pos.y > BoundsY.y - r)
                {
                    velocity.Value = new float2(velocity.Value.x, -velocity.Value.y);
                }

                if (pos.x < BoundsX.x + r || pos.x > BoundsX.y - r)
                {
                    velocity.Value = new float2(-velocity.Value.x, velocity.Value.y);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            Vector2 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
            Vector2 topRight = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

            BoundingJob job = new BoundingJob()
            {
                BoundsX = new float2(bottomLeft.x, topRight.x),
                BoundsY = new float2(bottomLeft.y, topRight.y)
            };

            return job.Schedule(this, inputDependencies);
        }
    }
}
