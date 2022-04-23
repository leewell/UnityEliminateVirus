using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GermSpike : Enemy
{
    [Header("伤害值")]
    public int damageValue;

    public GameObject explosionEffect;

    protected override void Start()
    {
        base.Start();
        PoolManager.Instance.InitPool(explosionEffect, 1);
    }

    protected override void Attack()
    {
        base.Attack();
        if (Time.time - lastActTime < actRestTime)
        {
            return;
        }
        transform.LookAt(playerTrans);
        lastActTime = Time.time;
        playerTrans.GetComponent<DoctorController>().TakeDamage(damageValue);

        GameObject explosionObj = PoolManager.Instance.GetInstance<GameObject>(explosionEffect);
        explosionObj.transform.position = transform.position;
        explosionObj.transform.localScale = Vector3.one * 3;
        explosionObj.SetActive(true);
    }
}