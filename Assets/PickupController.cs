using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Networking;

public class PickupController : NetworkBehaviour
{
    public LayerMask m_PlayerMask;
    public Material m_HiddenMaterial;
    public Material m_ShowMaterial;
    public Material m_isEvilMaterial;

    public ParticleSystem evilExplode;

    private bool Taken;
    private Renderer m_Renderer;
    private Canvas m_UICanvas;
    private Text m_Text;
    private float TimeToReset = 15;

    System.Random rand = new System.Random(5);

    [SyncVar]
    private float m_PickupVariable;

    [SyncVar]
    private bool isEvil = false;

    // Use this for initialization
    void Start () {
        m_Renderer = gameObject.GetComponent<Renderer>();
        m_UICanvas = GetComponentInChildren<Canvas>();
        m_Text = m_UICanvas.GetComponentInChildren<Text>();
        CmdIsEvil();
    }

    [Command]
    void CmdIsEvil()
    {
        int random = UnityEngine.Random.Range(0, 100);
        if (random >= 90 && random <= 100)
        {
            RpcSetEvil(true);
        }
        else
        {
            RpcSetEvil(false);
        }
    }

    [ClientRpc]
    void RpcSetEvil(bool evil)
    {
        isEvil = evil;
        if (evil)
        {
            m_Renderer.material = m_isEvilMaterial;
        }
    }
	
	// Update is called once per frame
	void Update () {
		if (Taken)
        {
            TimeToReset -= Time.deltaTime;
            m_Text.text = Mathf.Round(TimeToReset).ToString();
            if (TimeToReset < 0)
            {
                Taken = false;
                m_Text.enabled = false;
                if (isEvil)
                {
                    m_Renderer.material = m_isEvilMaterial;
                }
                else
                {
                    m_Renderer.material = m_ShowMaterial;
                }
                TimeToReset = 15;
            }
        }
        if (isEvil)
        {
            m_Renderer.material = m_isEvilMaterial;
        }
	}

    private void OnTriggerEnter(Collider other)
    {
        if (!Taken)
        {
            // "Hide" the mesh
            m_Renderer.material = m_HiddenMaterial;
            // Make sure nobody else can take the pickup
            Taken = true;
            // Show countdown text
            m_Text.enabled = true;

            // Give pickup to player
            Rigidbody targetRigidBody = other.GetComponent<Rigidbody>();

            if (other.GetComponent<CarMovement>() != null)
            {
                other.GetComponent<CarMovement>().CmdIncreasePoints(200);
            }
            if (targetRigidBody && targetRigidBody.GetComponent<CarShooting>() != null)
            {
                if (isEvil)
                {
                    CarHealth targetCarHealh = other.GetComponent<CarHealth>();
                    evilExplode.Play();
                    targetCarHealh.CmdTakeDamage(25);
                }
                else
                {
                    m_PickupVariable = DateTime.Now.Millisecond % 10.0f;

                    if (m_PickupVariable < 3)
                    {
                        CarShooting targetShooting = targetRigidBody.GetComponent<CarShooting>();
                        targetShooting.GetNewPickup(0);
                    }
                    else if (m_PickupVariable >= 3 && m_PickupVariable <= 7)
                    {
                        CarShooting targetShooting = targetRigidBody.GetComponent<CarShooting>();
                        targetShooting.GetNewPickup(1);
                    }
                    else
                    {
                        CarShooting targetShooting = targetRigidBody.GetComponent<CarShooting>();
                        targetShooting.GetNewPickup(2);
                    }
                }
            }
        }
    }
}
