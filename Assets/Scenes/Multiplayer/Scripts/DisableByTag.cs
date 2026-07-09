using UnityEngine;

public class DeactivateByTag : MonoBehaviour
{
    public void DeactivateObject(string tag)
    {
        GameObject target = GameObject.FindGameObjectWithTag(tag);
        //if (target != null)
        //{
        //    target.SetActive(false);
        //}
    }
}
