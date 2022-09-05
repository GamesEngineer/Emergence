using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RulesPanel : MonoBehaviour
{
    bool isOpen;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isOpen = !isOpen;
            Time.timeScale = isOpen ? 0.01f : 1f;

            if (!isOpen)
            {
                var rs = GetComponentsInChildren<RuleInput>();
                foreach (var r in rs)
                {
                    r.CommitChanges();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            var s = FindObjectOfType<Simulation>();
            s.RandomizeRules();
            var rs = GetComponentsInChildren<RuleInput>();
            foreach (var r in rs)
            {
                r.UpdateText();
            }
        }
        
        var p = transform.localPosition;
        p.y = Mathf.MoveTowards(p.y, isOpen ? 0f : -945f, Time.unscaledDeltaTime * 3000f);
        transform.localPosition = p;
    }
}
