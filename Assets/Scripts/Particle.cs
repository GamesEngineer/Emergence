using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

public struct Particle
{
    public enum Type { Red, Green, Blue, Yellow }; // type affects the particle's color and its behavior (simulation rules)
    private static readonly Color[] colors = new Color[4] { Color.red, Color.green, Color.blue, Color.yellow };

    public Type type;
    public float2 position;
    public float2 velocity;
    public float2 netForce;
    public Color color => colors[(int)type];

    public static float2 ClampMagnitude(float2 v, float maxMagnitude)
    {
        float magnitude = length(v);
        return (magnitude <= maxMagnitude) ? v : (v / magnitude);
    }

    /// <returns>force vector that particle 'p2' applies to particle 'p1'</returns>
    public static float2 ComputeForce(Particle p1, Particle p2, float maxForce, float maxDistance, float collisionDistance, float collisionForce)
    {
        float2 force = 0f;

        float2 delta = p1.position - p2.position;
        float distance = length(delta);
        distance = max(1e-8f, distance); // prevent divide by zero
        float2 direction = delta / distance;

        // Apply distance-based force
        if (distance < maxDistance)
        {
            force -= (maxForce / distance) * direction;
        }

        // Apply collision force
        if (distance < collisionDistance)
        {
            force += collisionForce * direction;
        }

        return force;
    }

    public void UpdateVelocityAndPosition(float deltaTime, float friction, float maxSpeed)
    {
        velocity *= Mathf.Clamp01(1f - friction * deltaTime); // friction
        velocity += netForce * deltaTime; // acceleration (particle mass is one unit, so F = mA becomes F = A)
        velocity = ClampMagnitude(velocity, maxSpeed);
        position += velocity * deltaTime; // translation
    }

    public void BounceOffWalls(float4 walls, float bounceVelocity)
    {
        if (position.x < walls.x) { position.x = walls.x; velocity.x = +bounceVelocity - velocity.x; }
        if (position.x > walls.y) { position.x = walls.y; velocity.x = -bounceVelocity - velocity.x; }
        if (position.y < walls.z) { position.y = walls.z; velocity.y = +bounceVelocity - velocity.y; }
        if (position.y > walls.w) { position.y = walls.w; velocity.y = -bounceVelocity - velocity.y; }
    }
}
