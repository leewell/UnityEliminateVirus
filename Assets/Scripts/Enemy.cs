using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 1f;
    [Header("被打死的粒子特效")]
    public ParticleSystem destroyEffect;
    [Header("血量")]
    public float initHealth = 10f;
    [Header("当前剩余血量-用于观察")]
    public float currentHealth;
    public EnemyState enemyState = EnemyState.Patrol;
    //上一次行为的时间(行为CD)
    protected float lastActTime;
    [Header("转向的持续时间")] //即多少秒后转向
    public float actRestTime = 4f;
    private Quaternion targetRotate;
    //初始位置
    protected Vector3 initPos;
    [Header("敌人的哪些轴可以旋转")]
    public EnemyFunction enemyFunction;
    [Header("攻击距离")]
    public float attackRange;
    [Header("追逐距离")]
    public float chaseRange;
    [Header("安全距离")]
    public float safeRange;
    protected Transform playerTrans;
    protected Rigidbody rigid;
    //当前移动速度
    protected float currentMoveSpeed;
    //初始追逐范围
    protected float initChaseRange;
    //初始安全范围
    protected float initSafeRange;
    //初始攻击范围
    protected float initAttackRange;
    //是否可以转向计时器
    private float canTurnTimer;

    public GameObject smokeColliderGo;
    public SoundPlayer soundPlayer;
    [Header("受伤音效")]
    public AudioClip hurtClip;
    [Header("攻击音效")]
    public AudioClip attackClip;

    private void Awake()
    {
        playerTrans = GameObject.FindGameObjectWithTag("Player").transform;
        rigid = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        currentHealth = initHealth;
        if (destroyEffect)
        {
            PoolManager.Instance.InitPool(destroyEffect, 4);
        }
        initPos = transform.position;
        initChaseRange = chaseRange;
        initSafeRange = safeRange;
        initAttackRange = attackRange;
        currentMoveSpeed = moveSpeed;
        canTurnTimer = Time.time;
    }

    protected virtual void Update()
    {
        CheckDistance();
        EnemyAct();
    }

    public virtual void TakeDamage(float damageValue)
    {
        currentHealth -= damageValue;
        RecoverRange();
        if (soundPlayer)
        {
            soundPlayer.PlayRandomSound();
        }
        else
        {
            AudioSourceManager.instance.PlaySound(hurtClip);
        }
        
        if (currentHealth > 0)
        {
            return;
        }
        if (destroyEffect)
        {
            ParticleSystem ps = PoolManager.Instance.GetInstance<ParticleSystem>(destroyEffect);
            ps.transform.position = transform.position;
            ps.time = 0;
            ps.Play();
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 敌人的行为
    /// </summary>
    protected virtual void EnemyAct()
    {
        switch (enemyState)
        {
            case EnemyState.Patrol:
                Move();
                if (Time.time - lastActTime > actRestTime)
                {
                    lastActTime = Time.time;
                    targetRotate = Quaternion.Euler(GetRandomEuler());
                }
                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Return:
                ReturnToInitPos();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 移动
    /// </summary>
    private void Move()
    {
        transform.Translate(transform.forward * currentMoveSpeed * Time.deltaTime, Space.World);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotate, 0.1f);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (enemyState == EnemyState.Patrol && collision.gameObject.layer != 11 && Time.time - canTurnTimer > 3f)
        {
            canTurnTimer = Time.time;
            targetRotate = Quaternion.LookRotation(-transform.forward, transform.up);
            lastActTime = Time.time;
        }else if (enemyState == EnemyState.Chase || enemyState == EnemyState.Attack || enemyState == EnemyState.Return)
        {
            rigid.isKinematic = true;
            Invoke("CloseIsKinematicState",1.5f);
        }
    }

    /// <summary>
    /// 延迟恢复刚体动力
    /// </summary>
    private void CloseIsKinematicState()
    {
        rigid.isKinematic = false;
    }

    /// <summary>
    /// 返回到初始位置
    /// </summary>
    protected virtual void ReturnToInitPos()
    {
        transform.Translate(transform.forward * currentMoveSpeed * Time.deltaTime, Space.World);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotate, 0.1f);
        if (Vector3.Distance(transform.position, initPos) <= 1f)
        {
            enemyState = EnemyState.Patrol;
        }
    }

    /// <summary>
    /// 检查与某一个对象的距离，并转换敌人状态
    /// </summary>
    protected virtual void CheckDistance()
    {
        if (enemyState == EnemyState.Return)
        {
            return;
        }
        float distance = Vector3.Distance(transform.position, playerTrans.position);
        if (distance <= attackRange && enemyFunction.canAttack) // 攻击
        {
            enemyState = EnemyState.Attack;
        }else if (distance <= chaseRange && enemyFunction.canChase) // 追逐
        {
            enemyState = EnemyState.Chase;
        }else if (distance >= safeRange && enemyFunction.canReturn) // 返回起始点
        {
            if (enemyState == EnemyState.Patrol && Vector3.Distance(transform.position, initPos) >= 8 || 
                enemyState == EnemyState.Chase)
            {
                enemyState = EnemyState.Return;
                targetRotate = Quaternion.LookRotation(initPos - transform.position, transform.up);
            }
        }
    }

    /// <summary>
    /// 获取各个轴的随机旋转角度
    /// </summary>
    /// <returns></returns>
    private Vector3 GetRandomEuler()
    {
        float x = 0, y = 0, z = 0;
        if (enemyFunction.canRotateX)
        {
            x = Random.Range(1, 5) * 90;
        }
        if (enemyFunction.canRotateY)
        {
            y = Random.Range(1, 5) * 90;
        }
        if (enemyFunction.canRotateZ)
        {
            z = Random.Range(1, 5) * 90;
        }
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 追逐玩家
    /// </summary>
    protected virtual void Chase()
    {
        transform.LookAt(playerTrans);
        transform.Translate(transform.forward * currentMoveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 攻击玩家
    /// </summary>
    protected virtual void Attack()
    {

    }

    /// <summary>
    /// 在烟雾里重新设置攻击距离与追逐距离
    /// </summary>
    public void SetRange(float value)
    {
        attackRange = chaseRange = value;
        enemyState = EnemyState.Idle;
    }

    public void RecoverRange()
    {
        attackRange = initAttackRange;
        chaseRange = initChaseRange;
        if (enemyFunction.canPatrol) // 当敌人碰到烟雾后，如果时可以巡逻的，就转化状态为巡逻状态
        {
            enemyState = EnemyState.Patrol;
        }
    }

    /// <summary>
    /// 用于烟雾弹检测
    /// </summary>
    public void SetSmokeColliderState(bool state)
    {
        smokeColliderGo.SetActive(state);
    }
}

/// <summary>
/// 敌人状态
/// </summary>
public enum EnemyState
{
    Patrol, // 巡逻状态
    Chase, // 追逐状态
    Attack, // 攻击状态
    Return, // 返回状态

    //Boss
    Idle,
    Warn,//警戒（播放动画，看向玩家）
    UseSkill
}

/// <summary>
/// 敌人功能
/// </summary>
[System.Serializable]
public struct EnemyFunction
{
    public bool canPatrol; // 是否可以巡逻
    public bool canChase; // 是否可以追逐
    public bool canAttack; // 是否可以攻击
    public bool canReturn; // 是否可以返回到起始点

    public bool canRotateX; // X轴可以旋转
    public bool canRotateY; // Y轴可以旋转
    public bool canRotateZ; // Z轴可以旋转
}