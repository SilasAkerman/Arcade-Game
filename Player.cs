using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))] // When this script is addeed, the script of PlayerController will be forced due to this requirement
[RequireComponent(typeof(GunController))]

public class Player : LivingEntity
{
    public float moveSpeed = 5f;

    public Crosshairs crosshairs;

    Camera viewCamera;
    PlayerController controller;
    GunController gunController;

    protected override void Start()
    {
        base.Start();
    }

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        gunController = GetComponent<GunController>();
        viewCamera = Camera.main;
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }

    void OnNewWave(int waveNumber)
    {
        health = startingHealth;
        gunController.EquipGun(waveNumber - 1);
    }

    void Update()
    {
        //Movement input
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = moveInput.normalized * moveSpeed;
        controller.SetVelocity(moveVelocity);

        //Look input
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.up * gunController.GunHeight);
        float rayDistance;

        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            //Debug.DrawLine(ray.origin, point, Color.red);
            controller.LookAt(point);
            crosshairs.transform.position = point;
            crosshairs.DetectTargets(ray);
            if ((new Vector2(point.x, point.z) - new Vector2(point.x, point.z)).sqrMagnitude > 1) // Threshold-distance determined through expermintation
            {
                gunController.Aim(point);
            }
        }

        //Weapon input
        if (Input.GetMouseButton(0))
        {
            gunController.OnTriggerHold();
        }

        if (Input.GetMouseButtonUp(0))
        {
            gunController.OnTriggerReleased();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            gunController.Reload();
        }

        if(transform.position.y < -10)
        {
            TakeDamage(health);
        }
    }

    protected override void Die()
    {
        AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
    }
}
