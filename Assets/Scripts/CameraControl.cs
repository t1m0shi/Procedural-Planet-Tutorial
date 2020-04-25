using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    protected Transform xformCam;
    [SerializeField] GameObject parent;
    protected Transform xformParent;

    protected Vector3 localRotation;
    protected float minDist = 2f; //130% planet radius?
    protected float maxDist = 5f; //500% planet radius
    protected float camDist;
    public float mouseSensitivity = 4f;
    public float scrollSensitivity = 2f;
    public float orbitDamp = 10f;
    public float scrollDamp = 6f;

    public bool CameraDisabled = false;

    void Start()
    {
        this.xformCam = this.transform;
        this.xformParent = this.transform.parent;
        localRotation.y = 32; //start angled 32 up
        camDist = maxDist;

    }

    void LateUpdate()
    {
        //can add cameradisabled stuff here

        if (!CameraDisabled)
        {
            if(Input.GetMouseButton(0))
            {
                localRotation.x += Input.GetAxis("Mouse X") * mouseSensitivity;
                localRotation.y -= Input.GetAxis("Mouse Y") * mouseSensitivity;

                //clamp y rotation to horizon to not flip at the top
                //localRotation.y = Mathf.Clamp(localRotation.y, 0f, 90f);
                //if x is in between 90 and -90

            }

            //zoom
            if(Input.GetAxis("Mouse ScrollWheel") != 0f)
            {
                float scrollAmount = Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
                scrollAmount *= (this.camDist * 0.3f); //0.3f is just a magic #
                this.camDist += scrollAmount * -1f;
                this.camDist = Mathf.Clamp(this.camDist, minDist, maxDist);
            }
        }

        Quaternion qt = Quaternion.Euler(localRotation.y, localRotation.x, 0);
        this.xformParent.rotation = Quaternion.Lerp(this.xformParent.rotation, qt, Time.deltaTime * orbitDamp);

        if(this.xformCam.localPosition.z != this.camDist * -1f)
        {
            this.xformCam.localPosition = new Vector3(0f, 0f, Mathf.Lerp(this.xformCam.localPosition.z, this.camDist * -1f, Time.deltaTime * scrollDamp));
        }
    }

}