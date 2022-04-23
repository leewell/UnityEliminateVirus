using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GermSlime : Enemy
{
    [Header("伤害值")]
    public int damageValue;
    protected override void Attack()
    {
        if (Time.time - lastActTime < actRestTime)
        {
            return;
        }
        transform.LookAt(playerTrans);
        lastActTime = Time.time;
        playerTrans.GetComponent<DoctorController>().TakeDamage(damageValue);
    }
}