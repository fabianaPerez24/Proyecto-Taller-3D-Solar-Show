using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] float bulletSpeed = 10f;

    [SerializeField] GameObject redTowerPrefab;
    [SerializeField] GameObject blueTowerPrefab;
    [SerializeField] Transform towerSpawn;

    PlayerHealth player;
    void Start()
    {
        player = GetComponent<PlayerHealth>();
    }
    void Update()
    {
        if (!IsOwner) return;
        if(Input.GetKeyDown(KeyCode.Space))
        {
            ShootServerRpc(shootPoint.position, shootPoint.forward);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            CreateTowerServerRpc(towerSpawn.position, towerSpawn.forward);
        }
    }

    [ServerRpc]
    void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));

        bullet.GetComponent<NetworkObject>().Spawn();

        bullet.GetComponent<Bullets>().owner = NetworkObject;

        Rigidbody rb =bullet.GetComponent<Rigidbody>();
        
        rb.linearVelocity = dir * bulletSpeed;
    }

    [ServerRpc]
    void CreateTowerServerRpc(Vector3 pos, Vector3 dir)
    {
        GameObject towerPrefabSpawn;

        if (player.team == 0)
        {
            towerPrefabSpawn = redTowerPrefab;
        }
        else
        {
            towerPrefabSpawn = blueTowerPrefab;
        }

        GameObject tower = Instantiate(
            towerPrefabSpawn,
            pos,
            Quaternion.LookRotation(dir)
        );

        tower.GetComponent<NetworkObject>().Spawn();

        tower.GetComponent<TowerLife>().owner = NetworkObject;
        //tower.GetComponent<TowerShoot>().owner = NetworkObject;
    }


}
