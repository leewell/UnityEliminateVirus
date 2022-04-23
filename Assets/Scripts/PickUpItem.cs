using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpItem : MonoBehaviour
{
    [Header("可拾取武器的自转速度")]
    public float rotateSpeed = 50f;
    [Header("捡起武器音效")]
    public AudioClip pickupClip;

    public int itemID = -1;

    private void Update()
    {
        transform.eulerAngles += new Vector3(0, rotateSpeed * Time.deltaTime, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Doctor")
        {
            AudioSourceManager.instance.PlaySound(pickupClip);
            DoctorController doctorController = other.GetComponent<DoctorController>();
            doctorController.PickUpWeapon(itemID);
            //不自动切换武器
            //doctorController.ChangeCurrentWeapon(true);
            Destroy(gameObject);
        }
    }
}