using UnityEngine;

public class DeactivateByTag : MonoBehaviour
{
    public void DeactivateObject(string tag)
    {
        GameObject.FindGameObjectWithTag(tag).SetActive(false);
    }
}
