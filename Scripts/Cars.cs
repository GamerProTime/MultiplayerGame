using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cars : MonoBehaviour
{
    public float speed;
    public TrailRenderer trail;

    private void Update()
    {
        trail = GetComponent<TrailRenderer>();
        transform.Translate(speed * Vector3.forward * Time.deltaTime);

        if (transform.position.z < -8)
        {
            GameObject clone = Instantiate(gameObject, transform.position = new Vector3(transform.position.x, transform.position.y, 7), Quaternion.identity);
            Destroy(gameObject);
        }
            
        if (transform.position.z > 8)
        {
            GameObject clone = Instantiate(gameObject, transform.position = new Vector3(transform.position.x, transform.position.y, -7), Quaternion.identity);
            Destroy(gameObject);
        }

    }
}
