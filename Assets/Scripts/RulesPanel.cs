using UnityEngine;

public class RulesPanel : MonoBehaviour
{
    private bool isPanelOpen = true;

    private void Start()
    {
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
            var s = FindObjectOfType<Simulation>();
            s.RandomizeRules();
            var rules = GetComponentsInChildren<RuleInput>();
            foreach (var rule in rules)
            {
                rule.UpdateText();
            }
        }
        
        // Slide the panel up/down in response to its open/closed state
        var p = transform.parent.localPosition;
        p.y = Mathf.MoveTowards(p.y, isPanelOpen ? 0f : -1050f, Time.unscaledDeltaTime * 3000f);
        transform.parent.localPosition = p;
    }

    private void SetPanelState(bool openPanel)
    {
        isPanelOpen = openPanel;
        Time.timeScale = isPanelOpen ? 0.05f : 1f; // slow time when the panel is open

        if (!isPanelOpen)
        {
            var rules = GetComponentsInChildren<RuleInput>();
            foreach (var rule in rules)
            {
                rule.CommitChanges();
            }
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
