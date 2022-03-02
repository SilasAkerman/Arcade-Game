using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public Transform weaponHold;
    public Gun[] allGuns;

    Gun equippedGun;

    void Start()
    {
        
    }

    public void EquipGun(Gun gunToEquip)
    {
        if (equippedGun != null) Destroy(equippedGun.gameObject);

        equippedGun = Instantiate(gunToEquip, weaponHold.position, weaponHold.rotation); // The return type is Object, which will be cast to Gun. Only possible if the Gun component is attached
        // Instantiate will clone the script as well as the entire object hierarchy, which is why the GameObject is spawned
        equippedGun.transform.parent = weaponHold;
    }

    public void EquipGun(int weaponIndex)
    {
        EquipGun(allGuns[weaponIndex]);
    }

    public void OnTriggerHold()
    {
        if (equippedGun != null)
        {
            equippedGun.OnTriggerHold();
        }
    }

    public void OnTriggerReleased()
    {
        if (equippedGun != null)
        {
            equippedGun.OnTriggerRelease();
        }
    }

    public float GunHeight { get { return weaponHold.position.y; } }

    public void Aim(Vector3 aimPoint)
    {
        if (equippedGun == null) return;

        equippedGun.Aim(aimPoint);
    }

    public void Reload()
    {
        equippedGun?.Reload();
    }
}
