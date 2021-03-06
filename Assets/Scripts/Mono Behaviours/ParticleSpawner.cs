﻿using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

using Random = Unity.Mathematics.Random;

namespace QFSW.GravityDOTS
{
    public class ParticleSpawner : MonoBehaviour
    {
        [SerializeField] private int _particleCount = 100;
        [SerializeField] private float _spawnRate = 100;

        [SerializeField] private float _particleMaxSpeed = 3;
        [SerializeField] private float2 _particleMass = new float2(100, 1000);

        [SerializeField] private float _particleDensity = 1;

        [SerializeField] private Material _particleMaterial;
        [SerializeField] private Mesh _particleMesh;

        private float _remainingParticleSpawns;
        private EntityManager _entityManager;
        private EntityArchetype _particleType;

        private void Awake()
        {
            ComponentType[] particleComponents =
            {
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Scale),
                typeof(Velocity),
                typeof(Mass),
                typeof(Radius),
                typeof(Bounded),
                typeof(RenderBounds),
                typeof(RenderMesh),
            };

            _entityManager = World.Active.EntityManager;
            _particleType = _entityManager.CreateArchetype(particleComponents);

            World.Active.GetOrCreateSystem<CollideMergeSystem>().ParticleDensity = _particleDensity;

            SpawnParticles(_particleCount);
        }

        private void Update()
        {
            _remainingParticleSpawns += Time.deltaTime * _spawnRate;
            if (_remainingParticleSpawns > 1)
            {
                int spawnCount = (int)_remainingParticleSpawns;
                _remainingParticleSpawns -= spawnCount;

                SpawnParticles(spawnCount);
            }
        }

        [BurstCompile]
        private void SpawnParticles(int count)
        {
            float2 bottomLeft = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(0, 0));
            float2 topRight = (Vector2)Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

            RenderMesh rm = new RenderMesh
            {
                mesh = _particleMesh,
                material = _particleMaterial,
                castShadows = ShadowCastingMode.Off,
                receiveShadows = false
            };

            RenderBounds bounds = new RenderBounds { Value = { Extents = new float3(1, 1, 1) } };

            NativeArray<Entity> particles = new NativeArray<Entity>(count, Allocator.TempJob);
            _entityManager.CreateEntity(_particleType, particles);

            Random r = new Random((uint)(1 + count + Time.time * 1000));

            float2 minVal = new float2(-_particleMaxSpeed, -_particleMaxSpeed);
            float2 maxVel = new float2(_particleMaxSpeed, _particleMaxSpeed);

            for (int i = 0; i < particles.Length; i++)
            {
                float mass = r.NextFloat(_particleMass.x, _particleMass.y);
                float radius = math.pow(3 / (4 * math.PI) * mass / _particleDensity, 1f / 3f);
                float scale = radius * 2f;

                Translation pos = new Translation() { Value = new float3(r.NextFloat2(bottomLeft, topRight), 0f) };

                Entity particle = particles[i];
                _entityManager.SetComponentData(particle, pos);
                _entityManager.SetComponentData(particle, bounds);
                _entityManager.SetSharedComponentData(particle, rm);
                _entityManager.SetComponentData(particle, new Mass() { Value = mass });
                _entityManager.SetComponentData(particle, new Velocity() { Value = r.NextFloat2(minVal, maxVel) });
                _entityManager.SetComponentData(particle, new Radius() { Value = radius });
                _entityManager.SetComponentData(particle, new Scale() { Value = scale });

            }

            particles.Dispose();
        }
    }
}
