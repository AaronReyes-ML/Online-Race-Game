using UnityEngine;

public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_PlayerMask;
    public ParticleSystem m_ExplosionParticles;
    public AudioSource m_ExplosionAudio;
    public float m_MaxDamage = 30f;
    public float m_ExplosionForce = 1000f;
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;


    private void Start()
    {
        Destroy(gameObject, m_MaxLifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != null && other.isTrigger)
        {
            CarHealth targetCarHeath = other.GetComponent<CarHealth>();
            if (targetCarHeath.IsShielded())
            {
                targetCarHeath.RemoveShield();
            }
            else
            {
                Rigidbody rigidbody = other.GetComponent<Rigidbody>();
                Rigidbody thisRigidBody = GetComponent<Rigidbody>();
                rigidbody.AddForce(thisRigidBody.velocity * 10, ForceMode.Force);
                Destroy(gameObject);
            }
        }
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate the amount of damage a target should take based on it's position.

        Vector3 explosionToTarget = targetPosition - transform.position;

        float explosionDistance = explosionToTarget.magnitude;
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

        float damage = relativeDistance * m_MaxDamage;

        damage = Mathf.Max(0f, damage);

        return damage;
    }
}