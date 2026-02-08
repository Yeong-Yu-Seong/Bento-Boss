using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EarningsTracker : MonoBehaviour
{
    public static EarningsTracker Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Assign the TextMeshPro object for the money display")]
    [SerializeField] private TextMeshProUGUI profitText;
    
    [Tooltip("Assign the Slider object")]
    [SerializeField] private Slider progressSlider;
    
    [Tooltip("Assign the 'Fill' image inside the Slider to change color on completion")]
    [SerializeField] private Image sliderFillImage;

    [Header("Billboard Settings")]
    [SerializeField] private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private float startingProfit = 0f;
    [SerializeField] private float dailyGoal = 50f;
    [SerializeField] private Color defaultColor = Color.yellow;
    [SerializeField] private Color goalReachedColor = Color.green;

    private float _currentProfit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        _currentProfit = startingProfit;
        InitializeUI();
    }

    private void Update()
    {
        if (mainCamera == null) return;

        Vector3 direction = mainCamera.transform.position - transform.position;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void InitializeUI()
    {
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = dailyGoal;
        }

        if (sliderFillImage != null)
        {
            sliderFillImage.color = defaultColor;
        }

        UpdateUI();
    }

    public void AddProfit(float amount)
    {
        if (amount <= 0f) return;

        _currentProfit += amount;
        UpdateUI();

        if (_currentProfit >= dailyGoal)
        {
            OnGoalReached();
        }
    }

    private void UpdateUI()
    {
        if (profitText != null)
        {
            profitText.text = $"${_currentProfit:F2} / ${dailyGoal:F2}";
        }

        if (progressSlider != null)
        {
            progressSlider.value = _currentProfit;
        }

        if (sliderFillImage != null)
        {
            sliderFillImage.color = _currentProfit >= dailyGoal ? goalReachedColor : defaultColor;
        }
    }

    private void OnGoalReached()
    {
        // Logic for when goal is hit
    }
}