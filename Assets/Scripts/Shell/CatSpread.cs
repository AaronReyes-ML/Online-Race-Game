using UnityEngine;

public class CatSpread : MonoBehaviour
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
        if (other.GetComponent<CarMovement>() != null && other.isTrigger)
        {
            CarHealth targetCarHeath = other.GetComponent<CarHealth>();
            if (targetCarHeath.IsShielded())
            {
                targetCarHeath.RemoveShield();
                Destroy(gameObject);
            }
            else
            {
                CarMovement targetCarMovement = other.GetComponent<CarMovement>();
                targetCarMovement.SetCatOn();
            }
        }
    }
}