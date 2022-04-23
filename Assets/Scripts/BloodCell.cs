using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodCell : Enemy
{
    [Header("加血量")]
    public int addValue = 10;
    [Header("加血量音效")]
    public AudioClip audioClip;

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (collision.gameObject.tag == "Player")
        {
            AudioSourceManager.instance.PlaySound(audioClip);
            playerTrans.GetComponent<DoctorController>().TakeDamage(-addValue);
            gameObject.SetActive(false);
        }
    }

    public override void TakeDamage(float damageValue)
    {
        base.TakeDamage(damageValue);
        if (currentHealth <= 0)
        {
            AudioSourceManager.instance.PlaySound(audioClip);
            playerTrans.GetComponent<DoctorController>().TakeDamage(-addValue);
        }
    }
}