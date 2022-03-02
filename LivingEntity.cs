using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamageable
{
    public float startingHealth;

    //[SerializeField] // Will show up in the inspector
    public float health { get; protected set; } // protected: Only available to child classes, or derived classes. Esentially private otherwise
    protected bool dead;

    public event System.Action OnDeath;

    protected virtual void Start() // why public? Maybe protected instead?
    {
        health = startingHealth;
    }

    public virtual void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        // The hit variable will later be used for particleEffects
        TakeDamage(damage);
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;

        if (health <= 0 && !dead)
        {
            Die(); // Ominous
        }
    }

    [ContextMenu("Self Destruct")]
    protected virtual void Die() 
    {
        dead = true;
        if (OnDeath != null) OnDeath();
        Destroy(gameObject);
    }
}
