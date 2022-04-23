using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody rigid;
    //爆炸特效预制体
    public ParticleSystem explosionEffect;
    //子弹拖尾特效
    public ParticleSystem bulletTrailEffect;
    [Header("爆炸时间")]
    public float destroyTime = 2.5f;
    //爆炸计时器
    private float destroyTimer;

    [Header("是否撞击到就销毁")]
    public bool destroyOnHit = false;
    [Header("爆炸伤害值")]
    public float damageValue = 10f;
    [Header("爆炸半径")]
    public int explosionRadius = 5;
    private Collider[] shpereCastPool = new Collider[10];
    //如果子弹时粒子，且需要大小的话就需要设置此值
    public ParticleSystem bulletSizeParticle;
    [Header("打到墙音效")]
    public AudioClip hitWallClip;
    [Header("手雷爆炸音效")]
    public AudioClip explosionClip;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        if (explosionEffect)
        {
            PoolManager.Instance.InitPool(explosionEffect, 8);
        }
        if (bulletTrailEffect)
        {
            bulletTrailEffect.time = 0;
            bulletTrailEffect.Play();
        }
    }

    private void Update()
    {
        destroyTimer += Time.deltaTime;
        if (destroyTimer > destroyTime)
        {
            if (destroyOnHit)
            {
                DestroyProjectile();
            }
            else
            {
                DestroyExplosionProjectile();
            }
        }
    }

    /// <summary>
    /// 子弹发射
    /// </summary>
    /// <param name="weapon">发射器</param>
    /// <param name="direction">发射方向</param>
    /// <param name="force">发射力大小</param>
    public void Launch(Weapon luancher,Vector3 direction,float force)
    {
        transform.position = luancher.GetShootPoint().position;
        transform.forward = luancher.GetShootPoint().forward;
        rigid.AddForce(direction * force);
    }

    /// <summary>
    /// 手雷爆炸
    /// </summary>
    private void DestroyExplosionProjectile()
    {
        if (explosionEffect != null)
        {
            ParticleSystem effect = PoolManager.Instance.GetInstance<ParticleSystem>(explosionEffect);
            effect.transform.position = transform.position;
            effect.gameObject.SetActive(true);
            effect.time = 0;
            effect.Play();
        }
        gameObject.SetActive(false);
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;
        destroyTimer = 0;

        if (damageValue > 0) // 屏蔽掉烟雾弹
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, shpereCastPool, 1 << 9);
            for (int i = 0; i < count; i++)
            {
                shpereCastPool[i].GetComponent<Enemy>().TakeDamage(damageValue);
            }
            int bossCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, shpereCastPool, 1 << 10);
            if (bossCount > 0)
            {
                shpereCastPool[0].GetComponentInParent<Boss>().TakeDamage(damageValue, transform.position);
            }
        }
        AudioSourceManager.instance.PlaySound(explosionClip);
    }

    /// <summary>
    /// 子弹爆炸
    /// </summary>
    private void DestroyProjectile(GameObject enemyGo = null)
    {
        if (explosionEffect != null)
        {
            ParticleSystem effect = PoolManager.Instance.GetInstance<ParticleSystem>(explosionEffect);
            effect.transform.position = transform.position;
            effect.gameObject.SetActive(true);
            effect.time = 0;
            effect.Play();
        }
        gameObject.SetActive(false);
        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;
        destroyTimer = 0;
        TakeDamage(enemyGo);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (destroyOnHit)
        {
            DestroyProjectile(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (destroyOnHit)
        {
            DestroyProjectile(other.gameObject);
        }
    }

    /// <summary>
    /// 设置聚能枪子弹大小
    /// </summary>
    public void SetBulletSize(float size)
    {
        bulletSizeParticle.startSize = size;
    }

    /// <summary>
    /// 设置聚能枪伤害值
    /// </summary>
    public void SetBulletDamageValue(float value)
    {
        damageValue = value;
    }

    private void TakeDamage(GameObject enemyGo = null)
    {
        if (enemyGo != null)
        {
            if(enemyGo.layer == 9)
            {
                enemyGo.GetComponent<Enemy>().TakeDamage(damageValue);
            }else if (enemyGo.layer == 10)
            {
                enemyGo.GetComponentInParent<Boss>().TakeDamage(damageValue, transform.position);
            }
            else // 如果时其他就默认打到墙了
            {
                AudioSourceManager.instance.PlaySound(hitWallClip);
            }
        }
    }
}