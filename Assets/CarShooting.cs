using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CarShooting : NetworkBehaviour
{
    public int m_PlayerNumber = 1;
    public GameObject m_Shell;
    public GameObject m_CatRigidyBody;
    public Transform m_FireTransform;
    public Slider m_AmmoSlider;
    public float m_MaxLaunchForce = 25f;
    public int m_StartAmmo = 10;
    public Image m_PickupImage;
    public Sprite m_Shield;
    public Sprite m_Harpoon;
    public Sprite m_Cat;
    public Sprite m_Empty;

    public GameObject m_Sheild;
    private MeshRenderer m_SheildMeshRenderer;

    private int m_Ammo;
    private string m_FireButton;
    private float m_CurrentLaunchForce;
    private float m_ChargeSpeed;
    private bool m_Fired;

    [SyncVar]
    private int m_currentWeapon = -1;


    private void OnEnable()
    {
        m_PickupImage.sprite = m_Empty;
    }


    private void Start()
    {
        m_FireButton = "Fire" + 1;
        m_SheildMeshRenderer = m_Sheild.GetComponent<MeshRenderer>();
        m_SheildMeshRenderer.enabled = false;
        SetPickupImage();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            if (Input.GetButtonDown(m_FireButton))
            {
                if (m_currentWeapon == 0)
                {
                    SetShield();
                }
                CmdFire();
            }

            SetPickupImage();
        }

        if (this.GetComponent<CarHealth>().IsShielded())
        {
            SetShield();
        }
        else if (!this.GetComponent<CarHealth>().IsShielded())
        {
            RemoveShield();
        }

    }

    [Command]
    private void CmdFire()
    {
        if (m_currentWeapon == 0)
        {
            SetShield();
            CarHealth carhealth = GetComponent<CarHealth>();
            carhealth.GetShield();
            
            m_currentWeapon = -1;
        }
        else if (m_currentWeapon == 1)
        {
            GameObject shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as GameObject;

            shellInstance.GetComponent<Rigidbody>().velocity = m_MaxLaunchForce * m_FireTransform.forward;
            NetworkServer.Spawn(shellInstance);

            m_currentWeapon = -1;
        }
        else if (m_currentWeapon == 2)
        {
            GameObject shellInstance = Instantiate(m_CatRigidyBody, m_FireTransform.position, m_FireTransform.rotation) as GameObject;

            shellInstance.GetComponent<Rigidbody>().velocity = m_MaxLaunchForce * m_FireTransform.forward * -1;
            NetworkServer.Spawn(shellInstance);

            m_currentWeapon = -1;
        }
    }

    public void GetNewPickup(int pickupID)
    {
        m_currentWeapon = pickupID;
        SetPickupImage();
    }

    public void SetShield()
    {
        m_SheildMeshRenderer.enabled = true;
    }

    public void RemoveShield()
    {
        m_SheildMeshRenderer.enabled = false;
    }

    public void SetPickupImage()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (m_currentWeapon == 0)
        {
            m_PickupImage.sprite = m_Shield;
        }
        else if (m_currentWeapon == 1)
        {
            m_PickupImage.sprite = m_Harpoon;
        }
        else if (m_currentWeapon == 2)
        {
            m_PickupImage.sprite = m_Cat;
        }
        else
        {
            m_PickupImage.sprite = m_Empty;
        }
    }
}