using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedZoneController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null)
        {
            Rigidbody targetRigidBody = other.GetComponent<Rigidbody>();
            CarMovement targetCarMovenet = targetRigidBody.GetComponent<CarMovement>();

            if (targetCarMovenet)
            {
                targetCarMovenet.SetInSpeedZone();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null)
        {
            Rigidbody targetRigidBody = other.GetComponent<Rigidbody>();
            CarMovement targetCarMovenet = targetRigidBody.GetComponent<CarMovement>();

            if (targetCarMovenet)
            {
                targetCarMovenet.SetOutSpeedZone();
            }
        }
    }
}
