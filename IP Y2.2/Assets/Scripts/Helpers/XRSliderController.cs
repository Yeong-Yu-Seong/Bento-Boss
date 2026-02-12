using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Slider))]
public class XRSliderController : MonoBehaviour
{
  [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable handleGrabInteractable;

  private Slider slider;
  private Camera xrCamera;
  private bool isGrabbed;
  private Vector3 grabStartPos;
  private float sliderStartValue;

  private void Awake()
  {
    slider = GetComponent<Slider>();

    if (handleGrabInteractable == null)
    {
      handleGrabInteractable = slider.handleRect.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    if (handleGrabInteractable == null)
    {
      Debug.LogError("Assign XRGrabInteractable on handle");
      enabled = false;
      return;
    }

    handleGrabInteractable.selectEntered.AddListener(OnGrabStart);
    handleGrabInteractable.selectExited.AddListener(OnGrabEnd);
  }

  private void Start()
  {
    xrCamera = Camera.main;
  }

  private void OnGrabStart(SelectEnterEventArgs args)
  {
    isGrabbed = true;
    grabStartPos = args.interactorObject.transform.position;
    sliderStartValue = slider.value;
  }

  private void OnGrabEnd(SelectExitEventArgs args)
  {
    isGrabbed = false;
  }

  private void Update()
  {
    if (!isGrabbed || handleGrabInteractable.interactorsSelecting.Count == 0) return;

    var interactor = handleGrabInteractable.interactorsSelecting[0];
    Vector3 currentPos = interactor.transform.position;
    Vector3 worldDelta = currentPos - grabStartPos;

    RectTransform sliderRect = slider.GetComponent<RectTransform>();
    Vector3 localDelta = sliderRect.InverseTransformDirection(worldDelta);

    float slideAxis = slider.direction == Slider.Direction.LeftToRight ||
                     slider.direction == Slider.Direction.RightToLeft
                     ? localDelta.x : localDelta.y;

    float sliderWidth = sliderRect.rect.width;
    float normalizedDelta = slideAxis / sliderWidth;

    if (slider.direction == Slider.Direction.RightToLeft ||
        slider.direction == Slider.Direction.TopToBottom)
    {
      normalizedDelta *= -1;
    }

    slider.value = Mathf.Clamp01(sliderStartValue + normalizedDelta);
  }

  private void OnDestroy()
  {
    if (handleGrabInteractable != null)
    {
      handleGrabInteractable.selectEntered.RemoveListener(OnGrabStart);
      handleGrabInteractable.selectExited.RemoveListener(OnGrabEnd);
    }
  }
}
