using System;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public class Simulation : MonoBehaviour
{
    [SerializeField, Range(1f, 1000f)] protected float maxForce = 100f;
    [SerializeField, Range(0.1f, 10f)] protected float maxSpeed = 5f;
    [SerializeField, Range(0f, 1f)] protected float friction = 0.5f;
    [SerializeField, Range(0.05f, 0.2f)] protected float collisionDistance = 0.1f;
    [SerializeField, Range(0f, 10000f)] protected float collisionForce = 1000f;
    [SerializeField] protected SpriteRenderer particleSprite_PF;
    [SerializeField] protected int numRedParticles = 1000;
    [SerializeField] protected int numGreenParticles = 1000;
    [SerializeField] protected int numBlueParticles = 1000;
    [SerializeField] protected int numYellowParticles = 1000;

    [SerializeField] protected Vector4 redForces = Vector4.one;
    [SerializeField] protected Vector4 redRadii = Vector4.one;
    [SerializeField] protected Vector4 greenForces = Vector4.one;
    [SerializeField] protected Vector4 greenRadii = Vector4.one;
    [SerializeField] protected Vector4 blueForces = Vector4.one;
    [SerializeField] protected Vector4 blueRadii = Vector4.one;
    [SerializeField] protected Vector4 yellowForces = Vector4.one;
    [SerializeField] protected Vector4 yellowRadii = Vector4.one;

    public enum ParticleType { Red, Green, Blue, Yellow };
    private Color[] ParticleColor = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };

    public struct Particle
    {
        public ParticleType type;
        public float2 position;
        public float2 velocity;
        public float2 netForce;
        public SpriteRenderer sprite;
    }

    public struct Rule
    {
        public float radius;
        public float force;
    }

    private Rule[,] rules;
    private Particle[] particles;
    private Unity.Mathematics.Random rng;
    private float4 walls = float4(-9f, +9f, -5f, +5f);

    private static float2 ClampMagnitude(float2 v, float maxMagnitude)
    {
        float magnitude = length(v);
        if (magnitude > maxMagnitude) v /= magnitude;
        return v;
    }

    private Particle CreateParticle(ParticleType type, float2 position, float2 velocity)
    {
        SpriteRenderer sprite = Instantiate(particleSprite_PF,
            new Vector3(position.x, position.y, 0f), Quaternion.identity);
        sprite.color = ParticleColor[(int)type];
        var particle = new Particle
        {
            type = type,
            position = position,
            velocity = velocity,
            netForce = 0f,
            sprite = sprite,
        };
        return particle;
    }

    private void CreateParticles(ParticleType type, int count, ref int index)
    {
        for (int i = 0; i < count; i++)
        {
            float2 pos = float2(
                rng.NextFloat(walls.x, walls.y),
                rng.NextFloat(walls.z, walls.w));
            float2 vel = rng.NextFloat2Direction();
            particles[index++] = CreateParticle(type, pos, vel);
        }
    }

    float2 ComputeForce(Particle p1, Particle p2)
    {
        float2 force = 0f;

        float2 delta = p2.position - p1.position;
        float distanceSqrd = dot(delta, delta);
        distanceSqrd = max(1e-8f, distanceSqrd); // prevent divide by zero
        float distance = sqrt(distanceSqrd);
        float2 direction = delta / distance;

        // Apply rule-based force
        Rule rule = rules[(int)p1.type, (int)p2.type];
        if (distance < rule.radius)
        {
            float ruleForce = rule.force;
            force += (ruleForce / distance) * direction;
            force = ClampMagnitude(force, maxForce);
        }

        // Apply collision force
        if (distance < collisionDistance)
        {
            force -= collisionForce * direction;
        }

        return force;
    }

    private void UpdateParticlePositions()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var particle = particles[i];
            particle.velocity *= Mathf.Clamp01(1f - friction * Time.deltaTime); // friction
            particle.velocity += particle.netForce * Time.deltaTime; // acceleration
            particle.velocity = ClampMagnitude(particle.velocity, maxSpeed);
            particle.position += particle.velocity * Time.deltaTime; // translation
            BounceOffWalls(ref particle.position, ref particle.velocity, walls);
            particle.sprite.transform.position = new Vector3(particle.position.x, particle.position.y, 0f);
            particles[i] = particle;
        }
    }

    private static void BounceOffWalls(ref float2 position, ref float2 velocity, float4 walls)
    {
        if (position.x < walls.x) { position.x = walls.x; velocity.x = +10f - velocity.x; }
        if (position.x > walls.y) { position.x = walls.y; velocity.x = -10f - velocity.x; }
        if (position.y < walls.z) { position.y = walls.z; velocity.y = +10f - velocity.y; }
        if (position.y > walls.w) { position.y = walls.w; velocity.y = -10f - velocity.y; }
    }

    private void ComputeParticleForces()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var p1 = particles[i];
            p1.netForce = 0f;
            for (int j = 0; j < particles.Length; j++)
            {
                if (i == j) continue;
                var p2 = particles[j];
                p1.netForce += ComputeForce(p1, p2);
            }
            particles[i] = p1;
        }
    }

    private void Start()
    {
        int numAllParticles = numRedParticles + numGreenParticles + numBlueParticles + numYellowParticles;
        particles = new Particle[numAllParticles];
        rng = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
        rules = new Rule[4, 4]
        {
            {
                new Rule{ radius = redRadii.x, force = redForces.x },
                new Rule{ radius = redRadii.y, force = redForces.y },
                new Rule{ radius = redRadii.z, force = redForces.z },
                new Rule{ radius = redRadii.w, force = redForces.w },
            },
            {
                new Rule{ radius = greenRadii.x, force = greenForces.x },
                new Rule{ radius = greenRadii.y, force = greenForces.y },
                new Rule{ radius = greenRadii.z, force = greenForces.z },
                new Rule{ radius = greenRadii.w, force = greenForces.w },

            },
            {
                new Rule{ radius = blueRadii.x, force = blueForces.x },
                new Rule{ radius = blueRadii.y, force = blueForces.y },
                new Rule{ radius = blueRadii.z, force = blueForces.z },
                new Rule{ radius = blueRadii.w, force = blueForces.w },

            },
            {
                new Rule{ radius = yellowRadii.x, force = yellowForces.x },
                new Rule{ radius = yellowRadii.y, force = yellowForces.y },
                new Rule{ radius = yellowRadii.z, force = yellowForces.z },
                new Rule{ radius = yellowRadii.w, force = yellowForces.w },

            },
        };

        int particleIndex = 0;
        CreateParticles(ParticleType.Red, numRedParticles, ref particleIndex);
        CreateParticles(ParticleType.Green, numGreenParticles, ref particleIndex);
        CreateParticles(ParticleType.Blue, numBlueParticles, ref particleIndex);
        CreateParticles(ParticleType.Yellow, numYellowParticles, ref particleIndex);
    }

    void Update()
    {
        ComputeParticleForces();
        UpdateParticlePositions();
    }
}
