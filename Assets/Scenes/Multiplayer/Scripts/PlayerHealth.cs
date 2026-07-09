using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] int maxHP = 100;
    public NetworkVariable<int> currentHp = new();
    [SerializeField] Image HPbar;
    [SerializeField] GameObject WorldHpBar;

    public int team;
    public override void OnNetworkSpawn()
    {
        currentHp.OnValueChanged += OnHealthChanged;
        if (IsServer) currentHp.Value = maxHP;
        UpdateHealthBar(currentHp.Value);
        if(IsOwner)
        {
            LocalHealthUI.Instance.SetupPlayer(this);
            WorldHpBar.SetActive(false);
        }

        if (OwnerClientId % 2 == 0)
        {
            team = 0;
        }
        else
        {
            team = 1;
        }
    }
    // Update is called once per frame
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        currentHp.Value -= damage;

        if (currentHp.Value <= 0)
        {
            Death();
        }
    }

    void OnHealthChanged(int oldHP, int newHP)
    {
        UpdateHealthBar(newHP);
    }
    void UpdateHealthBar(int health)
    {
        HPbar.fillAmount = (float)health / maxHP;
    }
    void Death()
        {
            GetComponent<NetworkObject>().Despawn();
        }
}
