using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider slider;
    [SerializeField] private WizardController wizard; // drag your player here (or leave null to auto-find)

    [Header("Smoothing")]
    [SerializeField] private bool smooth = true;
    [SerializeField, Range(1f, 30f)] private float smoothSpeed = 12f;

    private float targetValue; // 0..max

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!wizard)
        {
            var w = GameObject.FindWithTag("Player");
            if (w) wizard = w.GetComponent<WizardController>();
        }
    }

    void OnEnable()
    {
        if (wizard != null)
        {
            // Initialize from current health
            slider.minValue = 0f;
            slider.maxValue = wizard.MaxHealth;
            slider.value = wizard.CurrentHealth;
            targetValue = slider.value;

            wizard.onHealthChanged.AddListener(OnHealthChanged);
        }
    }

    void OnDisable()
    {
        if (wizard != null)
            wizard.onHealthChanged.RemoveListener(OnHealthChanged);
    }

    void Update()
    {
        if (!smooth) return;
        if (Mathf.Abs(slider.value - targetValue) > 0.001f)
        {
            slider.value = Mathf.MoveTowards(slider.value, targetValue, smoothSpeed * Time.unscaledDeltaTime);
        }
    }

    // Invoked by WizardController.onHealthChanged(current, max)
    private void OnHealthChanged(float current, float max)
    {
        // Keep slider in sync even if max HP changes at runtime
        if (!Mathf.Approximately(slider.maxValue, max))
            slider.maxValue = max;

        targetValue = Mathf.Clamp(current, slider.minValue, slider.maxValue);

        if (!smooth) slider.value = targetValue;
    }
}
