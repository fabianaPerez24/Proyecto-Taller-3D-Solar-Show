using UnityEngine;

public abstract class BaseVehicle : MonoBehaviour
{
    [SerializeField] protected Rigidbody rb;

    [Header("Movimiento")]
    [SerializeField] protected float currentSpeed = 0f;
    [SerializeField] protected bool onStun = false;

    protected virtual void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    protected abstract void ManageAcceleration();
    protected abstract void ManageSteering();
    protected abstract void ApplyVelocity();
}
