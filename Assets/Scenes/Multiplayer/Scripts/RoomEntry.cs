using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class RoomEntry : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public Button button;

    public void SetAction(string SalaName,Action IniciarSala)
    {
        Name.SetText(SalaName);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(IniciarSala.Invoke);
    }
}
