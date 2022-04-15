using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Jumpad : MonoBehaviourPunCallbacks
{
    [Range(100, 2000)]
    public float bounceHeight;
    public string particle;
    public GameObject location;
    Camera cam;

    private GameObject clone;

    private void OnEnable()
    {
        clone = PhotonNetwork.Instantiate(particle, location.transform.position, Quaternion.identity, 0);
    }

    private void Update()
    {
        if(cam == null)
            cam = FindObjectOfType<Camera>();

        if (cam == null)
            return;

        clone.transform.LookAt(cam.transform);
        clone.transform.Rotate(Vector3.up * 180);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 10)
        {
            collision.gameObject.GetComponent<Rigidbody>().AddForce(Vector3.up * bounceHeight);
        }
    }

}

