using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    [UpdateAfter(typeof(MovementSystem))]
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class BoundingBoxSystem : JobComponentSystem
    {
		[BurstCompile]
        private struct BoundingJob : IJobForEach<Velocity, Translation, Radius, Bounded>
        {
            public float2 BoundsX;
            public float2 BoundsY;

            public void Execute(ref Velocity velocity, ref Translation position,
                [ReadOnly] ref Radius radius, [ReadOnly] ref Bounded bounded)
            {
                float2 pos = position.Value.xy;
                float r = radius.Value;

                if (pos.x < BoundsX.x + r)
                {
                    position.Value = new float3(BoundsX.x + r, pos.y, 0);
                    velocity.Value = new float2(-velocity.Value.x, velocity.Value.y);
                }
                else if (pos.x > BoundsX.y - r)
                {
                    position.Value = new float3(BoundsX.y - r, pos.y, 0);
                    velocity.Value = new float2(-velocity.Value.x, velocity.Value.y);
                }

                if (pos.y < BoundsY.x + r)
                {
                    position.Value = new float3(pos.x, BoundsY.x + r, 0);
                    velocity.Value = new float2(velocity.Value.x, -velocity.Value.y);
                }
                else if (pos.y > BoundsY.y - r)
                {
                    position.Value = new float3(pos.x, BoundsY.y - r, 0);
                    velocity.Value = new float2(velocity.Value.x, -velocity.Value.y);
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
