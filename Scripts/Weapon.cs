using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Weapon : MonoBehaviourPunCallbacks
{
    #region Variables

    public Gun[] loadout;
    public Transform weaponParent;
    public GameObject bulletholePrefab;
    public GameObject bloodParticle;
    public GameObject t_newWeapon;
    public LayerMask canBeShot;
    public bool isAiming = false;

    private float currentCooldown;
    public GameObject currentWeapon;
    private int currentIndex;

    private bool isReloading = false;
    #endregion

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        Equip(0);
    }
    private void Start()
    {
        foreach (Gun a in loadout) a.Initialize();
        
    }
    void Update()
    {
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1)) { photonView.RPC("Equip", RpcTarget.All, 0); }
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2)) { photonView.RPC("Equip", RpcTarget.All, 1); }

        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                Aim((Input.GetMouseButton(1)));

                if(loadout[currentIndex].burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading)
                    {
                        if (loadout[currentIndex].FireBullet()) { photonView.RPC("Shoot", RpcTarget.All); }
                        else StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading)
                    {
                        if (loadout[currentIndex].FireBullet()) { photonView.RPC("Shoot", RpcTarget.All); }
                        else StartCoroutine(Reload(loadout[currentIndex].reload));
                    }
                }
                if(Input.GetKeyDown(KeyCode.R) && loadout[currentIndex].clipsize > loadout[currentIndex].clip) 
                    StartCoroutine(Reload(loadout[currentIndex].reload));

                //cooldown;
                if (currentCooldown > 0) currentCooldown -= Time.deltaTime;
            }
        }

        //weapon position elasticity
        currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
    }

    #endregion

    #region Private Methods

    IEnumerator Reload(float p_wait)
    {
        isReloading = true;
        currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;

    }

    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null)
        {
            if(isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }

        currentIndex = p_ind;

        t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        currentWeapon = t_newWeapon;
    }

    void Aim(bool p_isAiming)
    {
        isAiming = p_isAiming;
        Transform t_anchor = currentWeapon.transform.Find("Anchor");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

        if (p_isAiming)
        {
            //aim
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            //stop aiming
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
    }

    [PunRPC]
    void Shoot()
    {

        Transform t_spawn = transform.Find("Cameras/Normal Camera");

        //bloom
        Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
        t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
        t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
        t_bloom -= t_spawn.position;
        t_bloom.Normalize();

        RaycastHit t_hit = new RaycastHit();
        if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
        {
            if(t_hit.collider.gameObject.layer != 12)
            {
                GameObject t_newHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newHole, 5f);

            }

            if (photonView.IsMine)
            {
                //if shooting player
                if (t_hit.collider.gameObject.layer == 12)
                {
                    //Deal Damage
                    t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage);
                    photonView.RPC("SetParticle", RpcTarget.All, t_hit.point + t_hit.normal);
                }
            }
        }

        //gun fx
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;


        //cooldown
        currentCooldown = loadout[currentIndex].firerate;
    }

    [PunRPC]
    private void TakeDamage(int p_damage)
    {
        GetComponent<Player>().TakeDamage(p_damage);
    }

    [PunRPC]
    private void SetParticle(Vector3 p_location)
    {
        GameObject clone = Instantiate(bloodParticle, p_location, Quaternion.identity);
        Destroy(clone, 2.5f);
    }

    #endregion

    #region Public Methods

    public void RefreshAmmo(Text p_text)
    {
        if (p_text == null) return;
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");
    }

    public void DropWeapon()
    {
        GetComponent<Rigidbody>().useGravity = true;
        loadout[currentIndex].prefab.GetComponent<BoxCollider>().enabled = true;
    }

    #endregion
}
