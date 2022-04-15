using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Look : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables

    public static bool cursorLocked = true;

    public Transform player;
    public Transform cams;
    public Transform weapon;

    public GameObject head;

    public float xSensitivity;
    public float ySensitivity;
    public float maxAngle;

    private Quaternion camCenter;
    private float smoothRot = 5f;

    #endregion

    #region Monobehabiour Callbacks
    void Start()
    {
        camCenter = cams.localRotation;
    }
    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if (p_stream.IsWriting)
        {
           p_stream.SendNext(head.transform.rotation);
        }
        else
        {
           head.transform.rotation = (Quaternion)p_stream.ReceiveNext();
        }
    }
    void Update()
    {
        if (!photonView.IsMine) {  return; }

        SetY();
        SetX();

        UpdateCursorLock();
    }

    private void FixedUpdate()
    {
        //if (!photonView.IsMine) { head.transform.rotation = Quaternion.Lerp(head.transform.rotation, Quaternion.identity, Time.deltaTime * smoothRot); };
    }
    #endregion

    #region Private Methods
    void SetY()
    {
        float t_input = Input.GetAxisRaw("Mouse Y") * ySensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
        Quaternion t_delta = cams.localRotation * t_adj;

        if (Quaternion.Angle(camCenter, t_delta) < maxAngle)
        {
            cams.localRotation = t_delta;
            head.transform.rotation = cams.rotation;
        }

        weapon.rotation = cams.rotation;
    }

    void SetX()
    {
        float t_input = Input.GetAxisRaw("Mouse X") * xSensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = player.localRotation * t_adj;
        player.localRotation = t_delta;
    }

    void UpdateCursorLock()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = true;
            }
        }
    }
    #endregion

}
