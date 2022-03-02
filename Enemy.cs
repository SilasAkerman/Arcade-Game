using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : LivingEntity
{
    public enum State { Idle, Chasing, Attacking }
    State currentState;

    public ParticleSystem deathEffect;
    public ParticleSystem hitEffect;

    public static event System.Action OnDeathStatic;

    NavMeshAgent pathfinder;
    Transform target;
    LivingEntity targetEntity;

    Material skinMaterial;
    Color originalColor;

    float attackDistanceThreshold = .5f;
    float timeBetweenAttacks = 1;
    float damage = 1;

    float nextAttackTime;
    float myCollisionRadius;
    float targetCollisionRadius;

    bool hasTarget;

    void Awake()
    {
        pathfinder = GetComponent<NavMeshAgent>();

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
            targetEntity = target.GetComponent<LivingEntity>();
            //targetEntity.OnDeath += OnTargetDeath;

            //currentState = State.Chasing; // Don't need this in the awake
            hasTarget = true;

            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;

            //StartCoroutine(nameof(UpdatePath));
        }
        else { currentState = State.Idle; hasTarget = false; }
    }

    protected override void Start()
    {
        base.Start();
        //pathfinder = GetComponent<NavMeshAgent>();

        if (hasTarget) // hasTarget set in the Awake method
        {
            //target = GameObject.FindGameObjectWithTag("Player").transform;
            //targetEntity = target.GetComponent<LivingEntity>();
            targetEntity.OnDeath += OnTargetDeath;

            currentState = State.Chasing;
            //hasTarget = true;

            //myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            //targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;

            StartCoroutine(nameof(UpdatePath));
        }
        else { currentState = State.Idle; }
    }

    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, float enemyhealth, Color skinColor)
    {
        pathfinder.speed = moveSpeed;

        if (hasTarget)
        {
            damage = Mathf.Ceil(targetEntity.startingHealth / hitsToKillPlayer);
        }
        startingHealth = enemyhealth;

        skinMaterial = GetComponent<Renderer>().material;
        skinMaterial.color = skinColor;
        originalColor = skinMaterial.color;
    }

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        AudioManager.instance.PlaySound("Impact", transform.position);

        ParticleSystem newHitEffect = Instantiate(hitEffect, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection));
        Destroy(newHitEffect.gameObject, hitEffect.main.startLifetime.constant);

        if (damage >= health) // Death
        {
            OnDeathStatic?.Invoke();
            AudioManager.instance.PlaySound("Enemy Death", transform.position);

            ParticleSystem newDeathEffect = Instantiate(deathEffect, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection));
            newDeathEffect.GetComponent<ParticleSystemRenderer>().material = skinMaterial;
            Destroy(newDeathEffect.gameObject, deathEffect.main.startLifetime.constant);
        }
        base.TakeHit(damage, hitPoint, hitDirection);
    }

    void OnTargetDeath()
    {
        hasTarget = false;
        currentState = State.Idle;
    }

    void Update()
    {
        if (hasTarget)
        {
            if (Time.time > nextAttackTime)
            {
                float sqDstToTarget = (target.position - transform.position).sqrMagnitude;
                if (sqDstToTarget < Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2))
                {
                    nextAttackTime = Time.time + timeBetweenAttacks;
                    AudioManager.instance.PlaySound("Enemy Attack", transform.position);
                    StartCoroutine(nameof(Attack));
                }
            }
        }
    }

    IEnumerator Attack()
    {
        currentState = State.Attacking;
        pathfinder.enabled = false;

        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * (myCollisionRadius);

        float attackSpeed = 3;
        float percent = 0;

        skinMaterial.color = Color.red;
        bool hasAppliedDamage = false;

        while (percent <= 1)
        {
            if (percent >= 0.5f && !hasAppliedDamage) // This assumes that the attack is a guaranteed hit;
            {
                hasAppliedDamage = true;
                targetEntity.TakeDamage(damage);
            }

            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4; //Parabula with 0->1->0 in the span of 0->1
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        skinMaterial.color = originalColor;
        currentState = State.Chasing;
        pathfinder.enabled = true;
    }

    /// <summary>
    /// A delayed calculation of the <c>NavMeshAgent.SetDestination()</c>. refreshRate is hard-coded
    /// </summary>
    /// <returns></returns>
    IEnumerator UpdatePath()
    {
        float refreshRate = 0.2f;

        while (hasTarget)
        {
            if (currentState == State.Chasing)
            {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold/2);
                targetPosition.y = 0; // Target is on the ground

                if (!dead) // The couroutine could still try to acces the pathfinder even when the object is getting destroyed
                {
                    pathfinder.SetDestination(targetPosition);
                }
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
