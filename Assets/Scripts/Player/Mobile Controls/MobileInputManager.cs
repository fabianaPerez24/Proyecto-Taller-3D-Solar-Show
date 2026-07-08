using System;
using UnityEngine;

public class MobileInputManager : MonoBehaviour, IInputProvider
{
    private static MobileInputManager instance;
    public static MobileInputManager Instance => instance;

    private float horizontalInput;
    private float verticalInput;

    public float HorizontalInput => horizontalInput;
    public float VerticalInput => verticalInput;

    public event Action OnTurboTriggered;
    public event Action OnPauseTriggered;
    public event Action<bool> OnDriftStarted;
    public event Action<bool> OnDriftReleased;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void SetHorizontal(float value) => horizontalInput = value;
    public void SetVertical(float value) => verticalInput = value;

    public void TriggerTurbo()
    {
        OnTurboTriggered?.Invoke();
        Debug.Log("Señal de turbo enviada");
    }

    public void TriggerPause()
    {
        OnPauseTriggered?.Invoke();
        Debug.Log("Señal de pausa enviada");
    }

    public void StartDrift(bool isRight)
    {
        OnDriftStarted?.Invoke(isRight);
    }

    public void ReleaseDrift(bool isRight)
    {
        OnDriftReleased?.Invoke(isRight);
    }
}