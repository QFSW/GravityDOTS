﻿using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace QFSW.GravityDOTS
{
    [UpdateBefore(typeof(CollideMergeSystem))]
    [UpdateInGroup(typeof(FixedSimulationSystemGroup))]
    public class GravitySystem : JobComponentSystem
    {
        public const float Scale = 10E8f;
        public const float G = 6.6743015E-11f;

        // Each instance of the job call is an attractee
        // The inner loop handles attractors
        // This removes the race condition
        [BurstCompile]
        private struct GravityJob : IJobForEach<Mass, Translation, Radius, Velocity>
        {
            public int AttractorCount;
            public float DeltaTime;

            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<Mass> AttractorMass;

            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<Translation> AttractorPosition;

            [DeallocateOnJobCompletion, ReadOnly]
            public NativeArray<Radius> AttractorRadius;

            public void Execute([ReadOnly] ref Mass mass, [ReadOnly] ref Translation position,
                [ReadOnly] ref Radius radius, ref Velocity velocity)
            {
                float2 totalForce = new float2();
                for (int i = 0; i < AttractorCount; i++)
                {
                    float2 posdelta = (AttractorPosition[i].Value - position.Value).xy;

                    if (!(posdelta.x == 0 && posdelta.y == 0))
                    {
                        if (math.distancesq(AttractorPosition[i].Value, position.Value) < radius.Value * AttractorRadius[i].Value)
                            continue;

                        float distsq = posdelta.x * posdelta.x + posdelta.y * posdelta.y;
                        float distinv = math.rsqrt(distsq);
                        float fmag = Scale * G * mass.Value * AttractorMass[i].Value / distsq;
                        float2 dir = (AttractorPosition[i].Value - position.Value).xy * distinv;
                        float2 force = dir * fmag;
                        totalForce += force;
                    }
                }

                velocity.Value += totalForce / mass.Value * DeltaTime;
            }
        }

        private EntityQuery _attractorQuery;

        protected override void OnCreate()
        {
            _attractorQuery = GetEntityQuery(
                ComponentType.ReadOnly<Mass>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Radius>()
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            GravityJob job = new GravityJob()
            {
                DeltaTime = Time.deltaTime,
                AttractorCount = _attractorQuery.CalculateEntityCount(),
                AttractorMass = _attractorQuery.ToComponentDataArray<Mass>(Allocator.TempJob),
                AttractorPosition = _attractorQuery.ToComponentDataArray<Translation>(Allocator.TempJob),
                AttractorRadius = _attractorQuery.ToComponentDataArray<Radius>(Allocator.TempJob),
            };

            return job.Schedule(this, inputDependencies);
        }
    }
}
