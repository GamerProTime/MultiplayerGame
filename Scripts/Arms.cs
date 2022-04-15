using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arms : MonoBehaviour
{
    public GameObject right_fpstarget;
    private void Awake()
    {
        foreach (GameObject item in GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[])
        {
            if (item.name == "r_target")
            {
                if (item.transform.root.gameObject.layer == 10)
                {
                    print(item);
                    right_fpstarget = item;
                }
            }
        }
    }
}

