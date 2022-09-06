using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class Simulation : MonoBehaviour
{
    /// <summary>
    /// Rule defines the behavior of a specific type of Particle when in the
    /// presence of other particles. The types of particles that are associated
    /// with a Rule are inferred from the Rule's position (row,column) within
    /// the rules table of the simulation. The row index represents particles
    /// of "this" type and the column index represents particles of the "other"
    /// type.
    /// IMPORTANT! Rules are not usually symetric. So, rule (Red, Green) is not
    /// usually the same as rule (Green, Red).
    /// </summary>
    public struct Rule
    {
        public float radius; // "other" particles closer than the radius will exert the rule's force on "this" particle
        public float force; // +F is attraction, -F is repulsion
    }

    #region Inspector Parameters

    [Range(1f, 100000f)] public float maxForce = 10000f;
    [Range(0.1f, 10f)] public float maxSpeed = 5f;
    [Range(0f, 60f)] public float friction = 0.5f;
    [Range(0.05f, 0.2f)] public float collisionDistance = 0.1f;
    [Range(0f, 10000f)] public float collisionForce = 1000f;
    public SpriteRenderer particleSprite_PF;
    public int numRedParticles = 1000;
    public int numGreenParticles = 1000;
    public int numBlueParticles = 1000;
    public int numYellowParticles = 1000;
    public Vector4 redRadii = Vector4.one;
    public Vector4 redForces = Vector4.one;
    public Vector4 greenRadii = Vector4.one;
    public Vector4 greenForces = Vector4.one;
    public Vector4 blueRadii = Vector4.one;
    public Vector4 blueForces = Vector4.one;
    public Vector4 yellowRadii = Vector4.one;
    public Vector4 yellowForces = Vector4.one;

    [Tooltip("False = use one CPU thread to update the simulation\nTrue = partition the work across multiple Job threads which run concurrently")]
    public bool useJobs;

    #endregion

    private NativeList<Particle> particles;
    private List<SpriteRenderer> sprites;
    private Unity.Mathematics.Random rng;
    private float4 walls = float4(-8.5f, +8.5f, -4.75f, +4.75f);

    #region Particle Rules

    public NativeArray<Rule> rules;
    public static int GetRuleIndex(int row, int col) => row * 4 + col;
    public Rule GetRule(int row, int col) => rules[GetRuleIndex(row, col)];
    public void SetRule(int row, int col, Rule rule) => rules[GetRuleIndex(row, col)] = rule;
    public void RandomizeRules()
    {
        maxSpeed = rng.NextFloat(8f) + 2f;
        friction = rng.NextFloat(60f);
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int ruleIndex = GetRuleIndex(row, col);
                rules[ruleIndex] = new Rule
                {
                    radius = Mathf.Pow(rng.NextFloat(walls.y / 4f), 2f),
                    force = Mathf.Pow(rng.NextFloat(5f), 2f) - Mathf.Pow(rng.NextFloat(5f), 2f),
                };
            }
        }
    }
    public void CopyRulesFromInspector()
    {
        rules[0] = new Rule { radius = redRadii.x, force = redForces.x };
        rules[1] = new Rule { radius = redRadii.y, force = redForces.y };
        rules[2] = new Rule { radius = redRadii.z, force = redForces.z };
        rules[3] = new Rule { radius = redRadii.w, force = redForces.w };

        rules[4] = new Rule { radius = greenRadii.x, force = greenForces.x };
        rules[5] = new Rule { radius = greenRadii.y, force = greenForces.y };
        rules[6] = new Rule { radius = greenRadii.z, force = greenForces.z };
        rules[7] = new Rule { radius = greenRadii.w, force = greenForces.w };

        rules[8] = new Rule { radius = blueRadii.x, force = blueForces.x };
        rules[9] = new Rule { radius = blueRadii.y, force = blueForces.y };
        rules[10] = new Rule { radius = blueRadii.z, force = blueForces.z };
        rules[11] = new Rule { radius = blueRadii.w, force = blueForces.w };

        rules[12] = new Rule { radius = yellowRadii.x, force = yellowForces.x };
        rules[13] = new Rule { radius = yellowRadii.y, force = yellowForces.y };
        rules[14] = new Rule { radius = yellowRadii.z, force = yellowForces.z };
        rules[15] = new Rule { radius = yellowRadii.w, force = yellowForces.w };
    }

    #endregion

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

            SpriteRenderer sprite = Instantiate(particleSprite_PF, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            sprite.transform.parent = transform;
            sprite.color = particle.color;
            sprites.Add(sprite);
        }
    }

    private void UpdateAllParticlePositions()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var particle = particles[i];
            particle.UpdateVelocityAndPosition(Time.deltaTime, friction, maxSpeed);
            particle.BounceOffWalls(walls, bounceVelocity: 10f);
            particles[i] = particle; // NOTE: Particle is a value type, not a reference type, so we ahve to copy the changed object back into the array

            var sprite = sprites[i];
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
            int ruleIndex = GetRuleIndex((int)p1.type, (int)p2.type);
            Rule rule = rules[ruleIndex];
            p1.netForce += Particle.ComputeForce(p1, p2, rule.force, rule.radius, collisionDistance, collisionForce);
        }
        p1.netForce = Particle.ClampMagnitude(p1.netForce, maxForce);
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

    private struct Job_UpdateParticleNetForce : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Rule> rules;
        [ReadOnly] public NativeArray<Particle> particlesIn;
        [WriteOnly] public NativeArray<Particle> particlesOut;
        public float maxForce;
        public float collisionDistance;
        public float collisionForce;

        public void Execute(int particleIndex)
        {
            // The following is similar to UpdateParticle, but it only uses members of this job instance
            var p1 = particlesIn[particleIndex];
            p1.netForce = 0f;
            for (int j = 0; j < particlesIn.Length; j++)
            {
                if (particleIndex == j) continue;
                var p2 = particlesIn[j];
                int ruleIndex = GetRuleIndex((int)p1.type, (int)p2.type);
                Rule rule = rules[ruleIndex];
                p1.netForce += Particle.ComputeForce(p1, p2, rule.force, rule.radius, collisionDistance, collisionForce);
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
            maxForce = maxForce,
            collisionDistance = collisionDistance,
            collisionForce = collisionForce
        };
        var computeForces_jobHandle = computeForces_Job.Schedule(particles.Length, innerloopBatchCount: 10);
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
        CopyRulesFromInspector();
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
