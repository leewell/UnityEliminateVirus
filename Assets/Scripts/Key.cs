using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("钥匙的自转速度")]
    public float rotateSpeed = 50f;
    [Header("捡到钥匙的音效")]
    public AudioClip pickupKeyClip;

    private void Update()
    {
        transform.eulerAngles += new Vector3(0, rotateSpeed * Time.deltaTime, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            AudioSourceManager.instance.PlaySound(pickupKeyClip);
            Destroy(gameObject);
        }
    }
}