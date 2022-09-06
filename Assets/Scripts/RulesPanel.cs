using UnityEngine;

public class RulesPanel : MonoBehaviour
{
    private bool isPanelOpen;

    void Update()
    {
        // Show/hide the panel when the Space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPanelOpen = !isPanelOpen;
            Time.timeScale = isPanelOpen ? 0.05f : 1f; // slow time when the panel is open

            if (!isPanelOpen)
            {
                var rs = GetComponentsInChildren<RuleInput>();
                foreach (var r in rs)
                {
                    r.CommitChanges();
                }
            }
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
        var p = transform.localPosition;
        p.y = Mathf.MoveTowards(p.y, isPanelOpen ? 0f : -945f, Time.unscaledDeltaTime * 3000f);
        transform.localPosition = p;
    }
}
