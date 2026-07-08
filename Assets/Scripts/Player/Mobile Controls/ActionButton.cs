using UnityEngine;
using UnityEngine.EventSystems;

public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private enum ButtonActionType { Accelerate, Brake, Turbo, Pause }
    [SerializeField] private ButtonActionType actionType;

    public void OnPointerDown(PointerEventData eventData)
    {
        switch (actionType)
        {
            case ButtonActionType.Accelerate:
                MobileInputManager.Instance.SetVertical(1f);
                break;
            case ButtonActionType.Brake:
                MobileInputManager.Instance.SetVertical(-1f);
                break;
            case ButtonActionType.Turbo:
                MobileInputManager.Instance.TriggerTurbo();
                break;
            case ButtonActionType.Pause:
                MobileInputManager.Instance.TriggerPause();
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (actionType == ButtonActionType.Accelerate || actionType == ButtonActionType.Brake)
        {
            MobileInputManager.Instance.SetVertical(0f);
        }
    }
}