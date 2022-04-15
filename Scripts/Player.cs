using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{

    #region Variables
    public float speed;
    public float sprintModifier;
    public float crouchModifier;
    public float jumpForce;
    public float lengthOfSlide;
    public float slideModifier;
    public int max_health;
    public Camera normalCam;
    public GameObject cameraParent;
    public GameObject smokeTrail;
    public string ragdoll;
    public GameObject weaponParent;
    public Transform groundDetector;
    public Transform slideDetector;
    public LayerMask ground;

    public float slideAmount;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchingCollider;
    public GameObject slidingCollider;
    public GameObject body;
    public TwoBoneIKConstraint r_fpshandmove;

    private GameObject r_fpstarget;

    private Transform ui_healthbar;
    private Text ui_ammo;

    private Rigidbody rig;

    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrentPos;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private float movementCounter;
    private float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier = 1.5f;
    private Vector3 origin;

    private int current_health;

    private Manager manager;
    private Weapon weapon;
    private bool crouched;
    private Arms arm;
   // private GameObject gun;

    private bool sliding;
    private bool sprinting;
    private bool dead = false;
    private float slide_time;
    private Vector3 slide_dir;

    private float aimAngle;

    #endregion

    #region Photon Callbacks

    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));

            p_stream.SendNext(this.rig.position);
            p_stream.SendNext(this.rig.rotation);
            p_stream.SendNext(this.rig.velocity);
        }
        else
        {
            aimAngle = (int)p_stream.ReceiveNext() / 100f;

            networkPosition = (Vector3) p_stream.ReceiveNext();
            networkRotation = (Quaternion)p_stream.ReceiveNext();
            rig.velocity = (Vector3)p_stream.ReceiveNext();

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - p_message.timestamp));
            rig.position += rig.velocity * lag;
        }
    }

    #endregion

    #region MonoBehavior Callbacks

    void Awake()
    {
        if (photonView.IsMine)
        {
            weapon = GetComponent<Weapon>();
            FindTarget();
        }
    }
    private void Start()
    {
      
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        current_health = max_health;
       // fps_arms = GameObject.Find("/Arms");
        cameraParent.SetActive(photonView.IsMine);
      
        if (!photonView.IsMine)
        {
            body.GetComponent<SkinnedMeshRenderer>().enabled = true;
            gameObject.layer = 12;
            standingCollider.layer = 12;
            crouchingCollider.layer = 12;
            weaponParent.GetComponent<SkinnedMeshRenderer>().enabled = false;
        }

        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;

        if (Camera.main) Camera.main.enabled = false;

        /* GameObject[] items = weapon.loadout[0].prefab.GetComponentsInChildren<GameObject>();
         foreach(GameObject item in items)
         {
             print(item.name);
         }*/

        /*foreach (GameObject fooObj in GameObject.FindObjectsOfTypeAll<GameObject>())
         {
             print(fooObj)
         }
        GameObject[] rifles = GameObject.FindGameObjectsWithTag("Rifle");
        print("rifles " + rifles);
        foreach(GameObject rifle in rifles)
        {
            print(rifle.name + " " + rifle.transform.parent.name);
        }*/

       

        weaponParentOrigin = weaponParent.transform.localPosition;
        weaponParentCurrentPos = weaponParentOrigin;

        if (photonView.IsMine)
        {
            ui_healthbar = GameObject.Find("HealthBar").transform;
            ui_ammo = GameObject.Find("AmmoText").GetComponent<Text>();
            rig = GetComponent<Rigidbody>();
            body.GetComponent<SkinnedMeshRenderer>().enabled = false;
            RefreshHealthbar();
            weaponParent.GetComponent<SkinnedMeshRenderer>().enabled = true;
            //Invoke("FindTarget", 0.5f);
        }
    }

    private void Update()
    {
        rig = GetComponent<Rigidbody>();
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }
        
        //UI Refreshes
        RefreshHealthbar();
        weapon.RefreshAmmo(ui_ammo);

        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);

        //States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        //Crouching
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        //Jumping
        if (isJumping)
        {
            if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
            rig.AddForce(Vector3.up * jumpForce);
        }

        //Headbob
        if (sliding) 
        {
            Headbob(movementCounter, 0.15f, 0.075f);
            weaponParent.transform.localPosition = Vector3.Lerp(weaponParent.transform.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }
        else if (t_hmove == 0 && t_vmove == 0) 
        { 
            Headbob(idleCounter, 0.025f, 0.025f); 
            idleCounter += Time.deltaTime;
            weaponParent.transform.localPosition = Vector3.Lerp(weaponParent.transform.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
        }
        else if(!isSprinting && !crouched)
        {
            Headbob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 3;
            weaponParent.transform.localPosition = Vector3.Lerp(weaponParent.transform.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else if (crouched)
        {
            Headbob(movementCounter, 0.02f, 0.02f);
            movementCounter += Time.deltaTime * 4f;
            weaponParent.transform.localPosition = Vector3.Lerp(weaponParent.transform.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
        }
        else
        {
            Headbob(movementCounter, 0.15f, 0.075f);
            movementCounter += Time.deltaTime * 7f;
            weaponParent.transform.localPosition = Vector3.Lerp(weaponParent.transform.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
        }

      
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) {
            rig.position = Vector3.MoveTowards(rig.position, networkPosition, Time.fixedDeltaTime);
            rig.rotation = Quaternion.RotateTowards(rig.rotation, networkRotation, Time.fixedDeltaTime * 100.0f);

            return;
        }

        //Axis
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool slide = Input.GetKey(KeyCode.LeftControl);

        //States
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping; //&& isGrounded;
        bool isSliding = isSprinting && slide && !sliding;

        //Movement
        Vector3 t_direction = Vector3.zero;
        float t_adjustedSpeed = speed;

        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);
            slidingCollider.GetComponent<CapsuleCollider>().enabled = false;

            if (isSprinting)
            {
                sprinting = true;
                sliding = false;

                slidingCollider.GetComponent<CapsuleCollider>().enabled = true;

                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                t_adjustedSpeed *= sprintModifier;
                if (isGrounded)
                {
                    photonView.RPC("SetParticle", RpcTarget.All);
                }
            }
            else if (crouched)
            {
                slidingCollider.GetComponent<CapsuleCollider>().enabled = false;
                t_adjustedSpeed *= crouchModifier;
            }
        }
        else
        {
            sliding = true;
            sprinting = false;

            slidingCollider.GetComponent<CapsuleCollider>().enabled = true;

            print(slide_dir);
            t_direction = slide_dir;
            t_adjustedSpeed *= slideModifier;
            slide_time -= Time.deltaTime;

            if (slide_time <= 0)
            {
                sliding = false;
                weaponParentCurrentPos -= Vector3.down * (slideAmount - crouchAmount);
            }

            if (isGrounded)
            {
               photonView.RPC("SetParticle", RpcTarget.All);
            }
        }
           

        Vector3 t_targetVelocity = t_direction * t_adjustedSpeed * Time.deltaTime;
        t_targetVelocity.y = rig.velocity.y;
        rig.velocity = t_targetVelocity;

        //Sliding
        if (isSliding)
        {
            sliding = true;
            slide_dir = t_direction;
            slide_time = lengthOfSlide;
            weaponParentCurrentPos += Vector3.down * (slideAmount - crouchAmount);
            if (!crouched) photonView.RPC("SetCrouch", RpcTarget.All, true);
        }

        //Camera 
        if (sliding) 
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
        }
        else 
        {
            if (isSprinting) { normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f); }
            else { normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f); }

            if (crouched) { normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 6f); }
            else { normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f); }

            normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sliding)
        {
            slide_time = 0;
        }
        else if(sprinting)
        {
            sprinting = false;
        }
    }

    #endregion

    #region Private Methods

    void FindTarget()
    {
        print(weapon.currentWeapon);
        arm = weapon.currentWeapon.GetComponent<Arms>();
        print(arm.right_fpstarget.transform);
        r_fpshandmove.data.target = arm.right_fpstarget.transform;
        /* foreach (GameObject item in GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[])
         {
            if(item.name == "r_target")
             {
                if (item.transform.root.gameObject.layer == 10)
                {
                     r_fpstarget = item;
                     r_fpshandmove.data.target = r_fpstarget.transform;
                 }
             }
         }*/
    }
    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.transform.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.transform.rotation = Quaternion.Slerp(weaponParent.transform.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.transform.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.transform.localEulerAngles = finalRotation;
    }
    void Headbob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (weapon.isAiming) t_aim_adjust = 0.1f;
        targetWeaponBobPosition = weaponParentCurrentPos + new Vector3(Mathf.Cos(p_z) * p_x_intensity * t_aim_adjust, Mathf.Sin(p_z * 2) * p_y_intensity * t_aim_adjust, 0);
    }

    void RefreshHealthbar()
    {
        if (ui_healthbar == null) return;
        float t_health_ratio = (float)current_health / (float)max_health;
        ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio, 1, 1), Time.deltaTime * 8f);
    }

    [PunRPC]
    void SetCrouch (bool p_state)
    {
        if (crouched == p_state) return;

        crouched = p_state;

        if (crouched)
        {
            standingCollider.SetActive(false);
            crouchingCollider.SetActive(true);
            weaponParentCurrentPos += Vector3.down * crouchAmount;
        }

        else
        {
            standingCollider.SetActive(true);
            crouchingCollider.SetActive(false);
            weaponParentCurrentPos -= Vector3.down * crouchAmount;
        }
    }

    [PunRPC]
    void SetParticle()
    {
        GameObject clone = Instantiate(smokeTrail, groundDetector.transform.position, Quaternion.identity);
        Destroy(clone, 2f);
    }

    #endregion

    #region Public Methods

    public void TakeDamage(int p_damage)
    {
        if (photonView.IsMine)
        {
           current_health -= p_damage;
            RefreshHealthbar();
            if(current_health <= 0)
            {
                if(dead == false)
                {
                    GameObject clone = PhotonNetwork.Instantiate(ragdoll, gameObject.transform.position, gameObject.transform.rotation);
                    print("Spawning New Ragdoll " + Time.time);
                    manager.CleanUp(clone);
                    PhotonNetwork.Destroy(gameObject);
                    dead = true;
                    manager.Spawn();
                }
               
            }
        }
    }


    #endregion

}
