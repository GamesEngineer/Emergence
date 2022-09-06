using TMPro;
using UnityEngine;

public class RuleInput : MonoBehaviour
{
    public TMP_InputField radiusInput;
    public TMP_InputField forceInput;
    public Simulation sim;
    private int row;
    private int col;

    private void Awake()
    {
        sim = FindObjectOfType<Simulation>();
    }

    private void Start()
    {
        row = transform.GetSiblingIndex() / 4;
        col = transform.GetSiblingIndex() % 4;
        UpdateText();
    }

    public Simulation.Rule Rule
    {
        get => sim.GetRule(row, col);
        set => sim.SetRule(row, col, value);
    }

    public void UpdateText()
    {
        radiusInput.SetTextWithoutNotify(Rule.radius.ToString("f2"));
        forceInput.SetTextWithoutNotify(Rule.force.ToString("f2"));
    }

    public void CommitChanges()
    {
        SetRadius(radiusInput.text);
        SetForce(forceInput.text);
    }

    public void SetRadius(string value)
    {
        if (!float.TryParse(value, out float radius))
        {
            return;
        }
        var rule = Rule;
        rule.radius = radius;
        Rule = rule;
    }
    
    public void SetForce(string value)
    {
        if (!float.TryParse(value, out float force))
        {
            return;
        }
        var rule = Rule;
        rule.force = force;
        Rule = rule;
    }
}
