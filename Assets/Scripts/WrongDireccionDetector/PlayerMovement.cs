using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{        
    private Material material;
    [SerializeField] AlertaUI alerta;
    //public AlertaUI alertaUI;
    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            material = meshRenderer.material;
        }
    }

    void Update()
    {
        if (material == null || alerta == null || ListaDeCheckpoints.Instance == null)
        {
            return;
        }

        CheckAngle();
    }

    void CheckAngle()
    {
        GameObject currentCheckpoint = ListaDeCheckpoints.Instance.GetCurrentCheckpoint();
        if (currentCheckpoint == null)
        {
            return;
        }

        Transform Checkpoint = currentCheckpoint.transform;
        float angulo = Vector3.Dot(Checkpoint.forward, transform.right);
        if(angulo<-0.8)
        {
            material.color = Color.red;
            alerta.UpdateText("Direccion Contraria");
        }
        else
        {
            alerta.UpdateText(string.Empty);
            material.color = Color.white;
        }
        //Vector3.Angle(transform.forward, ListaDeCheckpoints.Instance.GetCurrentCheckpoint().transform.up);
        //if (Vector3.Angle(transform.forward,ListaDeCheckpoints.Instance.GetCurrentCheckpoint().transform.up)>90)
        //{
        //    GetComponent<MeshRenderer>().material = wrongDirection;

        //}
        //else
        //{
        //   // GetComponent<MeshRenderer>().material = normal;
        //}
    }
}