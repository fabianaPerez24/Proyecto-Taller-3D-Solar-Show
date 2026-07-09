using UnityEngine;
using UnityEngine.UI;

public class LocalHealthUI : MonoBehaviour
{
    [SerializeField] Image hpBar;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] float maxHp = 100f;

    public static LocalHealthUI Instance;

    private void Awake()
    {
        Instance = this;
    }
   public void SetupPlayer(PlayerHealth player)
    {
        playerHealth = player;

        playerHealth.currentHp.OnValueChanged += OnHealthChanged;
        UpdateHealthBar(playerHealth.currentHp.Value);
    }
    void OnHealthChanged(int oldHP, int newHP)
    {
        UpdateHealthBar(newHP);
    }

    void UpdateHealthBar(int health)
    {
        hpBar.fillAmount = (float)health / maxHp;
    }

}
