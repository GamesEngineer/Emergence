using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RulesPanel : MonoBehaviour
{
    public Simulation simulation;
    private bool isPanelOpen = true;

    public TMP_InputField maxSpeed_InputField;
    public Slider maxSpeed_Slider;

    public TMP_InputField friction_InputField;
    public Slider friction_Slider;

    public string MaxSpeed 
    {
        get => simulation.simRules.maxSpeed.ToString("f2");
        set => simulation.simRules.maxSpeed = float.Parse(value);
    }
    public float MaxSpeed_f 
    {
        get => simulation.simRules.maxSpeed;
        set => simulation.simRules.maxSpeed = value;
    }
    
    public string Friction 
    {
        get => simulation.simRules.friction.ToString("f2");
        set => simulation.simRules.friction = float.Parse(value);
    }
    public float Friction_f 
    {
        get => simulation.simRules.friction;
        set => simulation.simRules.friction = value;
    }
    public void UpdateMaxSpeed()
    {
        maxSpeed_InputField.text = MaxSpeed;
        maxSpeed_Slider.value = MaxSpeed_f;
    }
    public void UpdateFriction()
    {
        friction_InputField.text = Friction;
        friction_Slider.value = Friction_f;
    }

    private void Start()
    {
        UpdateMaxSpeed();
        UpdateFriction();
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
        pos.y = Mathf.MoveTowards(pos.y, isPanelOpen ? 0f : Screen.height/2 + rect.sizeDelta.y/2 - 60f, Time.unscaledDeltaTime * 3000f);
        transform.localPosition = pos;
    }

    private void RandomizeRules()
    {
        simulation.RandomizeRules();
        UpdateMaxSpeed();
        UpdateFriction();
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
