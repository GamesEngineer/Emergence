using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class Simulation : MonoBehaviour
{
    #region Inspector Parameters

    public SimRules simRules;
    public SpriteRenderer particleSprite_PF;
    public int numRedParticles = 100;
    public int numGreenParticles = 100;
    public int numBlueParticles = 100;
    public int numYellowParticles = 100;
    [Range(0f, 100f)] public float bounceVelocity = 10f;

    [Tooltip("False = use one CPU thread to update the simulation\nTrue = partition the work across multiple Job threads which run concurrently")]
    public bool useJobs;

    [Range(1, 1000)] public int jobBatchCount = 10;

    #endregion

    private NativeArray<Rule> rules;
    private NativeList<Particle> particles;
    private List<SpriteRenderer> sprites;
    private Unity.Mathematics.Random rng;
    private float4 walls = float4(-8.5f, +8.5f, -4.75f, +4.75f);
    private bool isRuleTableChanged;

    public Rule GetRule(int row, int col) => simRules.GetRule(row, col);
    public void SetRule(int row, int col, Rule rule) 
    {
        simRules.SetRule(row, col, rule);
        isRuleTableChanged = true;
    }
    public void RandomizeRules()
    {
        simRules.RandomizeRules(ref rng, walls);
        isRuleTableChanged = true;
    }

    private void AddParticles(Particle.Type type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float2 position = float2(
                rng.NextFloat(walls.x, walls.y),
                rng.NextFloat(walls.z, walls.w));
            float2 velocity = rng.NextFloat2Direction();

            var particle = new Particle
            {
                type = type,
                position = position,
                velocity = velocity,
                netForce = 0f,
            };
            particles.Add(particle);

            SpriteRenderer sprite = Instantiate(particleSprite_PF,
                new Vector3(position.x, position.y, 0f),
                Quaternion.identity);
            sprite.transform.parent = transform;
            sprite.color = particle.color;
            sprites.Add(sprite);
        }
    }

    private void UpdateAllParticlePositions()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            Particle particle = particles[i];
            particle.UpdateVelocityAndPosition(Time.deltaTime, simRules.friction, simRules.maxSpeed);
            particle.BounceOffWalls(walls, bounceVelocity);
            particles[i] = particle; // NOTE: Particle is a value type, not a reference type, so we have to copy the changed object back into the array

            SpriteRenderer sprite = sprites[i];
            sprite.transform.position = new Vector3(particle.position.x, particle.position.y, 0f);
        }
    }

    #region Update particle forces on the main thread

    private void UpdateParticleNetForce(int particleIndex)
    {
        var p1 = particles[particleIndex];
        p1.netForce = 0f;
        for (int neighborIndex = 0; neighborIndex < particles.Length; neighborIndex++)
        {
            if (particleIndex == neighborIndex) continue; // a particle ignores itself
            var p2 = particles[neighborIndex];
            Rule rule = simRules.GetRule((int)p1.type, (int)p2.type);
            p1.netForce += Particle.ComputeForce(p1, p2, rule.force, rule.radius, SimRules.COLLISION_DISTANCE, SimRules.MAX_FORCE);
        }
        p1.netForce = Particle.ClampMagnitude(p1.netForce, SimRules.MAX_FORCE);
        particles[particleIndex] = p1;
    }

    private void UpdateAllParticleForces()
    {
        for (int particleIndex = 0; particleIndex < particles.Length; particleIndex++)
        {
            UpdateParticleNetForce(particleIndex);
        }
    }

    #endregion

    #region Update particle forces with a Unity "parallel for" job

    [Unity.Burst.BurstCompile(CompileSynchronously = true)]
    private struct Job_UpdateParticleNetForce : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Rule> rules;
        [ReadOnly] public NativeArray<Particle> particlesIn;
        [WriteOnly] public NativeArray<Particle> particlesOut;
        public float maxForce;
        public float collisionDistance;

        public void Execute(int particleIndex)
        {
            // The following is similar to UpdateParticle, but it only uses members of this job instance
            var p1 = particlesIn[particleIndex];
            p1.netForce = 0f;
            for (int j = 0; j < particlesIn.Length; j++)
            {
                if (particleIndex == j) continue;
                var p2 = particlesIn[j];
                int ruleIndex = SimRules.GetRuleIndex((int)p1.type, (int)p2.type); // NOTE: we cannot use simRules.GetRule(r,c) because simRules is a ref-type
                Rule rule = rules[ruleIndex];
                p1.netForce += Particle.ComputeForce(p1, p2, rule.force, rule.radius, collisionDistance, maxForce);
            }
            p1.netForce = Particle.ClampMagnitude(p1.netForce, maxForce);
            particlesOut[particleIndex] = p1;
        }
    }

    private void UpdateAllParticleForcesViaJobs()
    {
        var particlesCopy = new NativeArray<Particle>(particles.AsArray(), Allocator.TempJob);
        var computeForces_Job = new Job_UpdateParticleNetForce
        {
            particlesIn = particlesCopy,
            particlesOut = particles,
            rules = rules,
            maxForce = SimRules.MAX_FORCE,
            collisionDistance = SimRules.COLLISION_DISTANCE,
        };
        var computeForces_jobHandle = computeForces_Job.Schedule(particles.Length, jobBatchCount);
        // NOTE! Other work could be done here while we wait for the jobs to finish their work
        computeForces_jobHandle.Complete();
        particlesCopy.Dispose();
    }

    #endregion

    #region Unity Messages

    private void Awake()
    {
        int numAllParticles = numRedParticles + numGreenParticles + numBlueParticles + numYellowParticles;
        particles = new NativeList<Particle>(numAllParticles, Allocator.Persistent);
        sprites = new List<SpriteRenderer>(numAllParticles);
        rng = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
        rules = new NativeArray<Rule>(4 * 4, Allocator.Persistent);
        isRuleTableChanged = true;
    }

    private void Start()
    {
        AddParticles(Particle.Type.Red, numRedParticles);
        AddParticles(Particle.Type.Green, numGreenParticles);
        AddParticles(Particle.Type.Blue, numBlueParticles);
        AddParticles(Particle.Type.Yellow, numYellowParticles);
    }

    private void OnDestroy()
    {
        // IMPORTANT! Native collections must be manually disposed in order to prevent memory leaks
        particles.Dispose();
        rules.Dispose();
    }
    
    void Update()
    {
        if (isRuleTableChanged)
        {
            simRules.CopyRulesTableToNativeArray(rules);
            isRuleTableChanged = false;
        }

        if (useJobs)
        {
            UpdateAllParticleForcesViaJobs();
        }
        else
        {
            UpdateAllParticleForces();
        }

        UpdateAllParticlePositions();
    }

    #endregion
}
