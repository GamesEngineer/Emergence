using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Unity.Mathematics.math;

[CustomEditor(typeof(SimRules))]
public class SimRulesEditor : Editor
{
    private static string[] typeLabels = new string[] { "Red", "Green", "Blue", "Yellow" };
    private Unity.Mathematics.Random rng;
    private float4 walls = float4(-8.5f, +8.5f, -4.75f, +4.75f);

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SimRules rules = (SimRules)target;

        var firstColWidth = GUILayout.Width(80f);
        var colWidth = GUILayout.Width(50f);
        var tableWidth = GUILayout.Width(50f * 4f);
        var floatFieldStyle = new GUIStyle(GUI.skin.textField);
        floatFieldStyle.alignment = TextAnchor.MiddleRight;

        using (var rulesGridV = new EditorGUILayout.VerticalScope())
        {
            using (var rulesGridH = new EditorGUILayout.HorizontalScope(tableWidth))
            {
                EditorGUILayout.LabelField(string.Empty, firstColWidth);
                for (int headerCol = 0; headerCol < 4; headerCol++)
                {
                    EditorGUILayout.LabelField(typeLabels[headerCol], colWidth);
                }
            }
            for (int row = 0; row < 4; row++)
            {
                EditorGUILayout.Space();
                using (var rulesGridH = new EditorGUILayout.HorizontalScope(tableWidth))
                {
                    using (var ruleV = new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(typeLabels[row] + " radius", firstColWidth);
                        EditorGUILayout.LabelField(typeLabels[row] + " force", firstColWidth);
                    }
                    for (int col = 0; col < 4; col++)
                    {
                        int ruleIndex = SimRules.GetRuleIndex(row, col);
                        var rule = rules.rulesTable[ruleIndex];
                        using (var ruleV = new EditorGUILayout.VerticalScope())
                        {
                            rule.radius = EditorGUILayout.FloatField(rule.radius, floatFieldStyle, colWidth);
                            rule.force = EditorGUILayout.FloatField(rule.force, floatFieldStyle, colWidth);
                        }
                        rules.rulesTable[ruleIndex] = rule;
                    }
                }
            }
        }

        if (GUILayout.Button("Randomize", GUILayout.Width(340f)))
        {
            Undo.RecordObject(rules, "Randomize Rules");            
            rng = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks);
            rules.RandomizeRules(ref rng, walls);
        }
    }
}
