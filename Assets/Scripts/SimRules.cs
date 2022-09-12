using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
[Serializable]
public struct Rule
{
    public float radius; // "other" particles closer than the radius will exert the rule's force on "this" particle
    public float force; // +F is attraction, -F is repulsion
}

[CreateAssetMenu(fileName = "SimRules", menuName = "GameU/Particle Simulation Rules")]
public class SimRules : ScriptableObject
{
    [Range(0.1f, 10f)] public float maxSpeed = 5f;
    [Range(0.01f, 60f)] public float friction = 30f;
    public const float collisionDistance = 0.1f;
    public const float maxForce = 1000f;

    [HideInInspector]
    public Rule[] rulesTable = new Rule[4 * 4];
    public static int GetRuleIndex(int row, int col) => row * 4 + col;
    public Rule GetRule(int row, int col) => rulesTable[GetRuleIndex(row, col)];
    public void SetRule(int row, int col, Rule rule) => rulesTable[GetRuleIndex(row, col)] = rule;
    public static float Round(float value, int decimalPlaces) => (float)Math.Round(value, decimalPlaces);

    public void RandomizeRules(ref Unity.Mathematics.Random rng, float4 walls)
    {
        maxSpeed = Round(rng.NextFloat(8f) + 2f, 3);
        friction = Round(rng.NextFloat(60f), 3);
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                var rule = new Rule
                {
                    radius = Mathf.Pow(rng.NextFloat(walls.y / 4f), 2f),
                    force = Mathf.Pow(rng.NextFloat(5f), 2f) - Mathf.Pow(rng.NextFloat(5f), 2f),
                };
                // Truncate very small digits
                rule.radius = Round(rule.radius, 3);
                rule.force = Round(rule.force, 3);
                rulesTable[GetRuleIndex(row, col)] = rule;
            }
        }
    }
    public void CopyRulesTableToNativeArray(NativeArray<Rule> rulesCopy)
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int ruleIndex = GetRuleIndex(row, col);
                rulesCopy[ruleIndex] = rulesTable[ruleIndex];
            }
        }
    }
}
