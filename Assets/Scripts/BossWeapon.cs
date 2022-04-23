using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWeapon : MonoBehaviour
{
    [Header("伤害值")]
    public int damageValue = 10;
    [Header("技能球的移动速度")]
    public int moveSpeed = 5;
    private DoctorController doctorController;
    //销毁时间
    private float destroyTime = 10;
    //销毁计时器
    private float timeValue;
    //爆炸销毁特效
    public ParticleSystem destroyEffect;
    [Header("boss武器是否可以移动")]
    public bool canMove = false;
    [Header("初始无敌时间")]
    public float initDamageTime = 2f;
    //持续伤害的计时器
    private float damageTimerVal;
    //是否会受到伤害
    private bool canTakeDamage = false;
    [Header("是否时烟雾技能武器")]
    public bool isSmoke = false;
    [Header("微调第三个boss的近战攻击偏移量")]
    public Vector3 offset;

    private void Start()
    {
        doctorController = GameObject.FindGameObjectWithTag("Player").GetComponent<DoctorController>();
        if (destroyEffect)
        {
            PoolManager.Instance.InitPool(destroyEffect,10);
        }
        damageTimerVal = initDamageTime;
    }

    private void Update()
    {
        //技能武器
        if (damageTimerVal <= 0)
        {
            damageTimerVal = initDamageTime;
            canTakeDamage = true;
        }
        else
        {
            damageTimerVal -= Time.deltaTime;
        }
        //近战武器
        if (!canMove)
        {
            return;
        }
        //远程武器
        transform.Translate(transform.forward * moveSpeed * Time.deltaTime, Space.World);
        if (timeValue > destroyTime)
        {
            gameObject.SetActive(false);
            gameObject.transform.SetParent(PoolManager.Instance.transform);
            timeValue = 0;
        }
        else
        {
            timeValue += Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            
            if (destroyEffect)
            {
                ParticleSystem particle = PoolManager.Instance.GetInstance<ParticleSystem>(destroyEffect);
                particle.gameObject.SetActive(true);
                particle.transform.position = transform.position + offset;
                particle.time = 0;
                particle.Play();
            }
            
            if (canMove)
            {
                gameObject.transform.SetParent(PoolManager.Instance.transform);
                timeValue = 0;
            }
            if (isSmoke)
            {
                if (canTakeDamage)
                {
                    doctorController.TakeDamage(damageValue);
                    damageTimerVal = initDamageTime;
                    canTakeDamage = false;
                }
            }
            else
            {
                doctorController.TakeDamage(damageValue);
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player")
        {
            if (canTakeDamage)
            {
                doctorController.TakeDamage(damageValue);
                canTakeDamage = false;
                damageTimerVal = initDamageTime;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            canTakeDamage = true;
            damageTimerVal = initDamageTime;
        }
    }
}