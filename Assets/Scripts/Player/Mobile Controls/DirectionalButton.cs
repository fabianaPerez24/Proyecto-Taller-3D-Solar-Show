using UnityEngine;
using UnityEngine.EventSystems;

public class DirectionalButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private bool isRightButton;
    [SerializeField] private float doubleTapTimeThreshold = 0.35f;

    private float lastTapTime;

    public void OnPointerDown(PointerEventData eventData)
    {
        float timeSinceLastTap = Time.time - lastTapTime;

        if (timeSinceLastTap <= doubleTapTimeThreshold)
        {
            MobileInputManager.Instance.StartDrift(isRightButton);
            Debug.Log($"Doble tap detectado en el botón {(isRightButton ? "Derecho" : "Izquierdo")}. Derrape activo");
        }

        MobileInputManager.Instance.SetHorizontal(isRightButton ? 1f : -1f);
        lastTapTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MobileInputManager.Instance.SetHorizontal(0f);
        MobileInputManager.Instance.ReleaseDrift(isRightButton);
    }
}