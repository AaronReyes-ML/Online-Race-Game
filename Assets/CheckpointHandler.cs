using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointHandler : MonoBehaviour {

    public int m_CheckpointNumber;

    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null)
        {
            Rigidbody targetRigidBody = other.GetComponent<Rigidbody>();
            CarMovement targetCarMovenet = targetRigidBody.GetComponent<CarMovement>();

            if (targetCarMovenet)
            {
                targetCarMovenet.CmdSetLastCheckpoint(m_CheckpointNumber);
                if (targetCarMovenet.m_lastCheckpoint == m_CheckpointNumber)
                {
                    targetCarMovenet.CmdIncreasePoints(500);
                }
            }
        }
    }
}
