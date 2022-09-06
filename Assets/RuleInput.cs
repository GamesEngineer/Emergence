using TMPro;
using UnityEngine;

public class RuleInput : MonoBehaviour
{
    public TMP_InputField radiusInput;
    public TMP_InputField forceInput;
    public Simulation sim;
    int row;
    int col;

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

    public void UpdateText()
    {
        radiusInput.SetTextWithoutNotify(MyRule.radius.ToString("f2"));
        forceInput.SetTextWithoutNotify(MyRule.force.ToString("f2"));
    }

    Simulation.Rule MyRule
    {
        get => sim.rules[row, col];
        set => sim.rules[row, col] = value;
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
        var rule = MyRule;
        rule.radius = radius;
        MyRule = rule;
    }
    
    public void SetForce(string value)
    {
        if (!float.TryParse(value, out float force))
        {
            return;
        }
        var rule = MyRule;
        rule.force = force;
        MyRule = rule;
    }
}
