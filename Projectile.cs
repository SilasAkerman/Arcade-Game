using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public LayerMask collisionMask;
    //public Color trailColor;
    public float speed = 10f;
    public float damage = 1;

    float lifetime = 3;
    float skinwidth = 0.1f; // Some margin if te enemy also moves towards the bullet and it goes through the edge, meaning that the raycist is inside and won't detect a collision

    void Start()
    {
        Destroy(gameObject, lifetime);

        Collider[] initialCollisions = Physics.OverlapSphere(transform.position, 0.1f, collisionMask); // For when the bullet spawns inside an enemy
        if (initialCollisions.Length > 0)
        {
            OnHitObject(initialCollisions[0], transform.position);
        }
        //GetComponentInChildren<TrailRenderer>().material.SetColor("_TintColor", trailColor);
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    void FixedUpdate()
    {
        float moveDistance = speed * Time.deltaTime;
        CheckCollisions(moveDistance);
        transform.Translate(Vector3.forward * moveDistance);
    }

    void CheckCollisions(float moveDistance)
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, moveDistance + skinwidth, collisionMask, QueryTriggerInteraction.Collide)) // also collides with triggers, not just colliders with physical barriers
        {
            OnHitObject(hit.collider, hit.point);
        }
    }

    

    void OnHitObject(Collider c, Vector3 hitPoint)
    {
        IDamageable damageableObject = c.GetComponent<IDamageable>(); // The script that implements the IDamageable interface
        if (damageableObject != null)
        {
            damageableObject.TakeHit(damage, hitPoint, transform.forward);
        }
        GameObject.Destroy(gameObject); // Destroy this bullet
    }
}
