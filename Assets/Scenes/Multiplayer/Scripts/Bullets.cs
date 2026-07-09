using UnityEngine;
using Unity.Netcode;

public class Bullets : NetworkBehaviour
{
    public NetworkObject owner;

        public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            Destroy(gameObject, 3f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if(other.CompareTag("Player"))
        {
            NetworkObject networkObject = other.GetComponent<NetworkObject>();

            if (networkObject == owner) return;

            other.GetComponent<PlayerHealth>().TakeDamage(40);
            GetComponent<NetworkObject>().Despawn();


            PlayerHealth target = other.GetComponent<PlayerHealth>();
            PlayerHealth shooter = owner.GetComponent<PlayerHealth>();
            if (target.team == shooter.team)
            {
                return;
            }
        }
    }
}
