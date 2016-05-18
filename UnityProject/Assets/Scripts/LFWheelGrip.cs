﻿using UnityEngine;
using System.Collections;

/// <summary>
/// WIP wheel collider.
/// </summary>
public class LFWheelGrip : MonoBehaviour {

    #region public congfiguable properties
    public float suspensionDistance = 2;
    public float wheelRadius = 0.5f;
    public float suspensionSpring = 2000;
    public float suspensionDamper = 200;
    public float steeringInput;
    public float lateralGrip = 2000;
    #endregion

    #region public for the moment to show in Unity editor
    public bool grounded;
    public ConfigurableJoint susJoint;
    public GameObject contact;
    public Rigidbody contactRb;
    public Vector3 lastHitPoint;
    public GameObject wheel;
    public Rigidbody rb;
    #endregion

    #region Internal private variables
    RaycastHit hit;
    int layerMask;
    #endregion

    void Awake()
    {
        //Set to the GO we're assigned to. Saves messing in editor and required for final product.
        print("Awake");
        wheel = this.transform.gameObject;
        rb = this.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Creates varies entities to model the suspension and sets start conditions
    /// </summary>
    void Start ()
    {
        // This ignores the "WheelCollidersIgnore" layer in KSP. KSP specific, should not be in this module. Used for convenience now as I copied KSP convention
        layerMask = 1 << 26;
        layerMask = ~layerMask;


        if(wheel.transform.localScale != Vector3.one)
        {
            print("The GameObject this script is aplied to should be set to scale 1,1,1!!!!");
        }

        // Needs to be an empty GO eventually - cube for convenience for now. Has to have Rigidybody for joint to attach to.
        contact = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(gameObject.GetComponent("BoxCollider"));
        //Destroy(gameObject.GetComponent("MeshRenderer"));
        contact.layer = 26;
        print(-wheel.transform.up);
        contact.transform.position = wheel.transform.position + -wheel.transform.up * (suspensionDistance + wheelRadius);
        contact.transform.rotation = wheel.transform.rotation;
        contactRb = contact.AddComponent<Rigidbody>();
        contactRb.mass = 0;
        
        // Creates the joint with carefully chosen parameters
        susJoint = this.gameObject.AddComponent<ConfigurableJoint>();
        susJoint.anchor = Vector3.zero;
        susJoint.axis = new Vector3(1, 0, 0);
        susJoint.autoConfigureConnectedAnchor = false;
        susJoint.secondaryAxis = new Vector3(0, 1, 0);

        susJoint.xMotion = ConfigurableJointMotion.Limited;
        susJoint.yMotion = ConfigurableJointMotion.Limited;
        susJoint.zMotion = ConfigurableJointMotion.Free;

        susJoint.angularXMotion = ConfigurableJointMotion.Free;
        susJoint.angularYMotion = ConfigurableJointMotion.Free;
        susJoint.angularZMotion = ConfigurableJointMotion.Free;
        susJoint.targetPosition = new Vector3(0, -suspensionDistance, 0); //essentially where to suspend to

        var SJLS = new SoftJointLimitSpring();
        SJLS.spring = 0;
        SJLS.damper = 0;
        susJoint.linearLimitSpring = SJLS;

        var SJL = new SoftJointLimit();
        SJL.bounciness = 0;
        SJL.limit = suspensionDistance; //this sets the hard limit, or what are often called bump stops
        SJL.contactDistance = 0;
        susJoint.linearLimit = SJL;

        //THIS CONTROLS LATERAL GRIP
        var XD = new JointDrive(); //only XDrive used for now. X and Z can be used independently if we wish
        XD.positionSpring = lateralGrip;
        XD.positionDamper = 100;
        XD.maximumForce = 10000000;
        susJoint.xDrive = XD;

        //THIS CONTROLS SUSPENSION SETTINGS
        var YD = new JointDrive(); //only yDrive used for now. X and Z can be used independently if we wish
        YD.positionSpring = suspensionSpring;
        YD.positionDamper = suspensionDamper;
        YD.maximumForce = 10000000;
        susJoint.yDrive = YD;

        susJoint.connectedBody = contactRb; //Attached the joint to the contact object

        susJoint.connectedAnchor = Vector3.zero;
        StartCoroutine(WaitForFixed()); // Start FixedUpdate when we're ready. Prevents FixedUpdate running before.
    }
	
	/// <summary>
    /// Moved into coroutine
    /// </summary>
	void FixedUpdate ()
    {


    }

    /// <summary>
    /// This wuold normally be in FixedUpdate, but a coroutine gives us a little more control in starting and stopping when we want
    /// </summary>
    /// <returns>Nothing, aside the the yield return to itself next physics frame</returns>
    IEnumerator WaitForFixed()
    {
        while(true)
        {
            if (Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit, suspensionDistance -.1f, layerMask))
            //if(Physics.SphereCast(wheel.transform.position, wheelRadius,-wheel.transform.up, out hit, suspensionDistance - wheelRadius, layerMask))
            {
                grounded = true;
                contactRb.isKinematic = true;

                contactRb.position = hit.point;
                contactRb.rotation = wheel.transform.rotation;

                lastHitPoint = hit.point;
            }
            else
            {
                grounded = false;
                contactRb.position = wheel.transform.position + -wheel.transform.up * (suspensionDistance + wheelRadius);
                contactRb.rotation = wheel.transform.rotation;
                contactRb.isKinematic = false;
            }
            yield return new WaitForFixedUpdate();
        }
    }
    /// <summary>
    /// Display a visual representation of the wheel in the editor. Unity has no inbuilt gizmo for 
    /// circles, so a sphere is used. Unlike the original WC, I've represented the wheel at top and bottom 
    /// of suspension travel
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if(wheel == null)
        {
            wheel = this.transform.gameObject;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(wheel.transform.position, wheelRadius);
        Gizmos.DrawWireSphere(wheel.transform.position + -wheel.transform.up * suspensionDistance, wheelRadius);
        Gizmos.DrawRay(wheel.transform.position, -wheel.transform.up * suspensionDistance);
    }

}