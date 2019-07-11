using UnityEngine;

public class DrivingOffroad : MonoBehaviour
{
    public LayerMask m_PlayerMask;

    private void Start()
    {

    }


    private void OnTriggerStay(Collider other)
    {
        if (other.attachedRigidbody)
        {
            CarHealth targetHealth = other.GetComponent<CarHealth>();
            CarMovement targetMovement = other.GetComponent<CarMovement>();

            if (targetMovement != null)
            {
                if (targetMovement.IsMoving())
                {
                    targetHealth.CmdTakeDamage(0.5f);
                }
            }
        }
    }
}