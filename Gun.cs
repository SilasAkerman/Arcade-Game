using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum FireMode { Auto, Burst, Single };
    public FireMode fireMode;

    public Transform[] projectileSpawns; // The position the projectile will be shot from.
    public Projectile projectile;
    public float msBetweenShots = 100f;
    public float muzzleVelocity = 35f;

    public int burstCount;
    public float msBetweenBursts = 50f;

    public int projectilesPerMag;
    int projectilesRemainingInMag;
    bool isReloading;
    public float reloadTime = .3f;

    [Header("Recoil")]
    public Vector2 kickMinMax = new Vector2(.05f, .2f);
    public Vector2 recoilAngleMinMax = new Vector2(3, 5);
    public float recoilMoveSettleTime = .1f;
    public float recoilRotationSettleTime = .1f;

    [Header("Effects")]
    public Transform shell;
    public Transform shellEjection;
    public AudioClip shootAudio;
    public AudioClip reloadAudio;
    MuzzleFlash muzzleflash;

    float nextShotTime;
    bool triggerReleasedSinceLastShot = true;

    Vector3 recoilSmoothDampVelocity;
    float recoilRotationSmoothDampVelocity;
    float recoilAngle;

    float totalGunAngle;

    void Start()
    {
        muzzleflash = GetComponent<MuzzleFlash>();
        projectilesRemainingInMag = projectilesPerMag;
    }

    void LateUpdate() // LateUpdate so that the LookAt function happens before it overrides the rotation
    {
        // animate recoil
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero, ref recoilSmoothDampVelocity, recoilMoveSettleTime);
        recoilAngle = Mathf.SmoothDamp(recoilAngle, 0, ref recoilRotationSmoothDampVelocity, recoilRotationSettleTime);
        totalGunAngle += recoilAngle;

        if(!isReloading && projectilesRemainingInMag == 0)
        {
            Reload();
        }

        transform.localEulerAngles = Vector3.left * totalGunAngle;
        totalGunAngle = 0;
    }

    /// <summary>
    /// Spawns a bullet projectile with the muzzleVelocity
    /// </summary>
    void Shoot()
    {
        if (isReloading) return;
        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + msBetweenShots / 1000;

        switch (fireMode)
        {
            case FireMode.Burst:
                if (!triggerReleasedSinceLastShot) return;
                StartCoroutine(BurstShoot());
                break;

            case FireMode.Single:
                if (!triggerReleasedSinceLastShot) return;
                FireBullet();
                break;

            case FireMode.Auto:
                FireBullet();
                break;
        }
    }

    void FireBullet()
    {
        if (projectilesRemainingInMag <= 0) return;

        foreach (Transform projectileSpawn in projectileSpawns)
        {
            if (projectilesRemainingInMag == 0) break;
            projectilesRemainingInMag--;

            Projectile newProjectile = Instantiate(projectile, projectileSpawn.position, projectileSpawn.rotation); // Again, instantiating a script results in the entire object also being instantiated. Casted to a Projectile script
            newProjectile.SetSpeed(muzzleVelocity);
        }

        Instantiate(shell, shellEjection.position, shellEjection.rotation);
        muzzleflash.Activate();

        transform.localPosition -= Vector3.forward * Random.Range(kickMinMax.x, kickMinMax.y);
        recoilAngle += Random.Range(recoilAngleMinMax.x, recoilAngleMinMax.y);
        recoilAngle = Mathf.Clamp(recoilAngle, 0, 30);

        AudioManager.instance.PlaySound(shootAudio, transform.position);
    }

    IEnumerator BurstShoot()
    {
        for (int i = 0; i < burstCount; i++)
        {
            FireBullet();
            yield return new WaitForSeconds(msBetweenBursts / 1000);
        }
    }

    public void Reload()
    {
        if (isReloading || projectilesRemainingInMag == projectilesPerMag) return;
        StartCoroutine(AnimateReload());
        AudioManager.instance.PlaySound(reloadAudio, transform.position);
    }

    IEnumerator AnimateReload()
    {
        isReloading = true;
        yield return new WaitForSeconds(.2f);

        float reloadSpeed = 1 / reloadTime;
        float percent = 0;
        Vector3 initialRot = transform.localEulerAngles;
        float maxReloadAngle = 360;

        while (percent < 1)
        {
            percent += reloadSpeed * Time.deltaTime;
            //float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4; //Parabula with 0->1->0 in the span of 0->1
            float reloadAngle = Mathf.Lerp(0, maxReloadAngle, percent);
            totalGunAngle += reloadAngle;

            yield return null;
        }
        transform.localEulerAngles = initialRot;

        isReloading = false;
        projectilesRemainingInMag = projectilesPerMag;
    }

    public void Aim(Vector3 aimPoint)
    {
        transform.LookAt(aimPoint); // Will rotate the gun, not the player
    }

    public void OnTriggerHold()
    {
        Shoot();
        triggerReleasedSinceLastShot = false;
    }

    public void OnTriggerRelease()
    {
        triggerReleasedSinceLastShot = true;
    }
}
