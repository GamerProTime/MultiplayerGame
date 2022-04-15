using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public string playerprefab;
    public Transform[] spawn_points;

    private SphereCollider sphere;

    //private Player player;

    public void Start()
    {
        Spawn();
    }

    void Update()
    {
        foreach(Transform spawn in spawn_points)
        {
           sphere = spawn.GetComponent<SphereCollider>();
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.layer == 12)
        {
            other.gameObject.SetActive(false);
        }
    }

    public void Spawn()
    {
        Transform t_spawn = spawn_points[Random.Range(0, spawn_points.Length)];
        GameObject newPlayer = PhotonNetwork.Instantiate(playerprefab, t_spawn.position, t_spawn.rotation);
    }
    public void CleanUp(GameObject ragdoll)
    {
        StartCoroutine("RemoveRagdoll", ragdoll);
    }

    IEnumerator RemoveRagdoll(GameObject p_victim)
    {
        yield return new WaitForSeconds(20f);
        PhotonNetwork.Destroy(p_victim);
    }
}
