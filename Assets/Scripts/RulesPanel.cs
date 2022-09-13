using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesPanel : MonoBehaviour
{
    public Simulation simulation;

    #region MaxSpeed
    public TMP_InputField maxSpeed_InputField;
    public Slider maxSpeed_Slider;

    public void MaxSpeedUpdated()
    {
        maxSpeed_InputField.text = MaxSpeed_AsString;
        maxSpeed_Slider.value = MaxSpeed;
    }

    public float MaxSpeed 
    {
        get => simulation.simRules.maxSpeed;
        set
        {
            if (simulation.simRules.maxSpeed == value) return;
            simulation.simRules.maxSpeed = value;
            MaxSpeedUpdated();
        }
    }

    public string MaxSpeed_AsString
    {
        get => simulation.simRules.maxSpeed.ToString("f2");
        set
        {
            if (!float.TryParse(value, out float newValue)) return;
            MaxSpeed = newValue;
        }
    }
    #endregion

    #region Friction
    public TMP_InputField friction_InputField;
    public Slider friction_Slider;

    public void FrictionUpdated()
    {
        friction_InputField.text = Friction_AsString;
        friction_Slider.value = Friction;
    }

    public float Friction
    {
        get => simulation.simRules.friction;
        set
        {
            if (simulation.simRules.friction == value) return;
            simulation.simRules.friction = value;
            FrictionUpdated();
        }
    }
    
    public string Friction_AsString 
    {
        get => simulation.simRules.friction.ToString("f3");
        set
        {
            if (!float.TryParse(value, out float newValue)) return;
            simulation.simRules.friction = newValue;
        }
    }
    #endregion

    private bool isPanelOpen = true;

    private void Start()
    {
        MaxSpeedUpdated();
        FrictionUpdated();
        SetPanelState(isPanelOpen);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }

        // Show/hide the panel when the Space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetPanelState(!isPanelOpen);
        }

        // Randomize the rules when the R key is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomizeRules();
        }

        // Slide the panel up/down in response to its open/closed state
        var pos = transform.localPosition;
        var rect = transform.GetComponent<RectTransform>();
        pos.y = Mathf.MoveTowards(pos.y, isPanelOpen ? 0f : Screen.height/2 + rect.sizeDelta.y/2 - 40f, Time.unscaledDeltaTime * 3000f);
        transform.localPosition = pos;
    }

    private void RandomizeRules()
    {
        simulation.RandomizeRules();
        MaxSpeedUpdated();
        FrictionUpdated();
        UpdateRulesTable();
    }

    private void UpdateRulesTable()
    {
        var rules = GetComponentsInChildren<RuleInput>();
        foreach (var rule in rules)
        {
            rule.UpdateText();
        }
    }

    private void SetPanelState(bool openPanel)
    {
        isPanelOpen = openPanel;
        Time.timeScale = isPanelOpen ? 0.05f : 1f; // slow time when the panel is open
        if (!isPanelOpen)
        {
            UpdateRulesTable();
        }
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
