using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class CarHealth : NetworkBehaviour
{
    public float m_StartingHealth = 100f;

    [SyncVar]
    private bool isInvincible;

    public Slider m_Slider;

    public Image m_FillImage;
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    private Rigidbody m_RigidBody;
    
    public AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;

    [SyncVar]
    private float m_CurrentHealth;  

    private bool m_Dead;
    private int collisionDamage = 3;

    [SyncVar]
    private bool isShielded = false;


    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
        m_RigidBody = GetComponent<Rigidbody>();

        m_ExplosionParticles.gameObject.SetActive(false);
        CmdTakeDamage(0);
    }

    [Command]
    public void CmdSetStartHealth(float value)
    {
        m_CurrentHealth = value;
    }

    [Command]
    public void CmdUpdateSliderMax(float vlaue)
    {
        m_Slider.maxValue = vlaue;
        RpcUpdateLocalSliderMax(vlaue);
    }

    [ClientRpc]
    public void RpcUpdateLocalSliderMax(float vlaue)
    {
        m_Slider.maxValue = vlaue;
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
    }

    [Command]
    public void CmdTakeDamage(float amount)
    {
        if (!isInvincible)
        {
            m_CurrentHealth -= amount;

            RpcSetHealthUI();

            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }
    }

    [ClientRpc]
    public void RpcSetHealthUI()
    {
        m_Slider.value = m_CurrentHealth;
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth/m_StartingHealth);
    }

    private void OnDeath()
    {
        if (!isInvincible)
        {
            m_Dead = true;
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);

            m_ExplosionParticles.Play();

            CarMovement carMovement = m_RigidBody.GetComponent<CarMovement>();
            carMovement.Disable();
            carMovement.CmdKill();
        }
    }

    public bool IsShielded()
    {
        return isShielded;
    }

    public void GetShield()
    {
        isShielded = true;
    }

    public void RemoveShield()
    {
        isShielded = false;
        CarShooting thisCar = GetComponent<CarShooting>();
        thisCar.RemoveShield();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null && other.gameObject != this && other.isTrigger)
        {
            Rigidbody targetRigidBody = other.GetComponent<Rigidbody>();
            if (targetRigidBody.GetComponent<CarHealth>() != null)
            {
                CarHealth targetCarHealth = targetRigidBody.GetComponent<CarHealth>();
                
                if (isShielded)
                {
                    if (!targetCarHealth.isShielded)
                    {
                        targetCarHealth.TakeCollisionDamage(2 * collisionDamage);
                    }
                    else
                    {
                        targetCarHealth.RemoveShield();
                    }
                }
                else
                {
                    if (!targetCarHealth.isShielded)
                    {
                        targetCarHealth.TakeCollisionDamage(collisionDamage);
                    }
                    else
                    {
                        targetCarHealth.RemoveShield();
                    }   
                }
            }
        }
    }

    public void TakeCollisionDamage(int damage)
    {
        if (!isInvincible)
        {
            CmdTakeDamage(damage);
        }
    }

    [ClientRpc]
    public void RpcSetInvincible()
    {
        isInvincible = true;
    }
}