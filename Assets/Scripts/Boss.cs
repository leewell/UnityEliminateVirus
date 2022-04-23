using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : Enemy
{
    protected Animator animator;
    //是否正在觉醒
    protected bool isWaking = false;
    //是否已经死亡
    protected bool isDead = false;
    public Light pointLight;
    //是否时愤怒状态
    private bool isAngryState = false;
    //是否有技能
    public bool hasSkill = false;
    [Header("攻击动画的动画片段")]
    public AnimationClip attackAnimationClip;
    [Header("攻击动画的播放速度")]
    public float attackSpeed = 1;
    [Header("钥匙游戏物体")]
    public GameObject keyGo;
    [Header("受到伤害僵直时间")]
    public float takeDamageTime = 1f;
    private float takeDamageTimer;
    public AudioClip deadClip;

    protected override void Start()
    {
        base.Start();
        animator = GetComponentInChildren<Animator>();
        animator.SetFloat("Born", -0.5f);
        enemyState = EnemyState.Idle;
        currentMoveSpeed = moveSpeed;
        if (attackAnimationClip)
        {
            animator.SetFloat("AttackSpeed", attackSpeed);
            actRestTime = attackAnimationClip.length / attackSpeed;
        }
        else
        {
            animator.SetFloat("AttackSpeed", 1);
        }
        takeDamageTimer = takeDamageTime;
    }

    protected override void Update()
    {
        if (isDead)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 0.3f);
            pointLight.intensity -= Time.deltaTime * 2;
            if (transform.position.y < -10)
            {
                Destroy(gameObject);
            }
            return;
        }
        takeDamageTimer -= Time.deltaTime;
        base.Update();
    }

    protected override void EnemyAct()
    {
        if (isWaking || isDead)
        {
            return;
        }
        base.EnemyAct();
        switch (enemyState)
        {
            case EnemyState.Patrol:
                break;
            case EnemyState.Chase:
                //animator.ResetTrigger("Hit");
                animator.ResetTrigger("Attack");
                if (hasSkill)
                {
                    animator.ResetTrigger("UseSkill");
                }
                animator.SetBool("Moving", true);
                if (isAngryState)
                {
                    animator.SetFloat("MoveSpeed", 1);
                    currentMoveSpeed = moveSpeed * 3;
                }
                break;
            case EnemyState.Attack:
                animator.SetBool("Moving", false);
                animator.ResetTrigger("Hit");
                if (isAngryState && hasSkill)
                {
                    animator.SetTrigger("UseSkill");
                }
                else
                {
                    animator.SetTrigger("Attack");
                }
                break;
            case EnemyState.Return:
                animator.SetBool("Moving", true);
                animator.ResetTrigger("Hit");
                break;
            case EnemyState.Idle:
                animator.SetBool("Moving", false);
                break;
            case EnemyState.Warn:
                break;
            case EnemyState.UseSkill:
                break;
            default:
                break;
        }
    }

    protected virtual void Warn()
    {
        if (isWaking)
        {
            return;
        }
        float warnValue = animator.GetFloat("Born");
        if (warnValue < 0)
        {
            animator.SetFloat("Born", 1);
            isWaking = true;
            animator.Play("Born", 0, 0);
            StartCoroutine(FinishWaking());
        }
        else
        {
            animator.SetTrigger("Roar");
            transform.LookAt(new Vector3(playerTrans.position.x, transform.position.y, playerTrans.position.z));
        }
    }

    private IEnumerator FinishWaking()
    {
        yield return new WaitForSeconds(9);
        animator.ResetTrigger("Hit");
        isWaking = false;
    }

    protected override void CheckDistance()
    {
        if (enemyState == EnemyState.Return || isWaking || isDead)
        {
            return;
        }
        float distance = Vector3.Distance(transform.position, playerTrans.position);
        if (distance < chaseRange * 1.5f && enemyState != EnemyState.Warn && enemyState != EnemyState.Attack && enemyState != EnemyState.Chase)
        {
            Warn();
            enemyState = EnemyState.Warn;
        }
        if (distance <= attackRange && enemyFunction.canAttack) // 攻击
        {
            enemyState = EnemyState.Attack;
        }
        else if (distance <= chaseRange && enemyFunction.canChase) // 追逐
        {
            enemyState = EnemyState.Chase;
        }
        else if (distance >= safeRange && enemyFunction.canReturn) // 返回起始点
        {
            if (enemyState == EnemyState.Chase && enemyFunction.canReturn)
            {
                enemyState = EnemyState.Return;
            }
        }
    }

    /// <summary>
    /// 重写返回到初始位置
    /// </summary>
    protected override void ReturnToInitPos()
    {
        if (!CanMove())
        {
            return;
        }
        currentHealth = initHealth;
        if (Vector3.Distance(transform.position, initPos) <= 2f)
        {
            enemyState = EnemyState.Idle;
            isAngryState = false;
            transform.eulerAngles = new Vector3(0, 180, 0);
            currentMoveSpeed = moveSpeed;
            chaseRange = initChaseRange;
            safeRange = initSafeRange;
            return;
        }
        transform.Translate(transform.forward * currentMoveSpeed * Time.deltaTime, Space.World);
        transform.LookAt(new Vector3(initPos.x, transform.position.y, initPos.z));
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damageValue">伤害值</param>
    /// <param name="hitPos">子弹撞击点位置</param>
    public void TakeDamage(float damageValue, Vector3 hitPos)
    {
        if (isWaking || isDead)
        {
            return;
        }
        currentHealth -= damageValue;
        base.RecoverRange();
        if (enemyState == EnemyState.Idle || enemyState == EnemyState.Warn)
        {
            chaseRange = initChaseRange * 2;
            safeRange = initSafeRange * 1.5f;
        }
        if (currentHealth < initHealth / 3 && !isAngryState) // 当boss血量低于了1/3时，变为愤怒状态
        {
            isAngryState = true;
        }
        if (takeDamageTimer < 0)
        {
            if (soundPlayer)
            {
                soundPlayer.PlayRandomSound();
            }
            else
            {
                AudioSourceManager.instance.PlaySound(hurtClip);
            }
            takeDamageTimer = takeDamageTime;
            animator.SetTrigger("Hit");
            animator.SetFloat("HitX", 0f);
            animator.SetFloat("HitY", 0f);
            float x = Vector3.Dot(transform.right, hitPos); // 判断左右
            float y = Vector3.Dot(transform.forward, hitPos - transform.position); // 判断前后（也可以用叉乘）
            if (CheckForwardBehindOrLeftFight(hitPos))
            {
                if (y > 0)
                {
                    //在前方
                    animator.SetFloat("HitY", 1);
                }
                else
                {
                    //在后方
                    animator.SetFloat("HitY", -1);
                }
            }
            else
            {
                if (x > 0)
                {
                    //在右方
                    animator.SetFloat("HitX", 1);
                }
                else
                {
                    //在左方
                    animator.SetFloat("HitX", -1);
                }
            }
        }
        if (currentHealth > 0)
        {
            animator.SetTrigger("Hit");
            return;
        }
        AudioSourceManager.instance.PlaySound(deadClip);
        animator.SetTrigger("Die");
        isDead = true;
        rigid.isKinematic = true;
        rigid.constraints = RigidbodyConstraints.FreezeAll;
        keyGo.SetActive(true);
        keyGo.transform.position = transform.position + Vector3.up * 2;
    }

    /// <summary>
    /// 检测是在前后，还是左右的优先级更高
    /// </summary>
    /// <param name="pos">子弹撞击点</param>
    /// <returns>true-前后；false-左右</returns>
    private bool CheckForwardBehindOrLeftFight(Vector3 targetPos)
    {
        float ZDistance = Mathf.Abs(targetPos.z - transform.position.z);
        float XDistance = Mathf.Abs(targetPos.x - transform.position.x);
        return ZDistance >= XDistance;
    }

    protected override void Attack()
    {
        if (Time.time - lastActTime < actRestTime)
        {
            return;
        }
        lastActTime = Time.time;
        transform.LookAt(new Vector3(playerTrans.position.x, transform.position.y, playerTrans.position.z));
    }

    /// <summary>
    /// boss是否可以进行移动（某些状态下禁止移动，比如吼叫/受击/攻击等）
    /// </summary>
    /// <returns>true-可以移动；false-禁止移动</returns>
    private bool CanMove()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash != Animator.StringToHash("Roar") &&
            info.shortNameHash != Animator.StringToHash("Hit") &&
            info.shortNameHash != Animator.StringToHash("Attack") &&
            info.shortNameHash != Animator.StringToHash("UseSkill");
    }

    protected override void Chase()
    {
        if (!CanMove())
        {
            return;
        }
        transform.LookAt(new Vector3(playerTrans.position.x, transform.position.y, playerTrans.position.z));
        transform.Translate(transform.forward * currentMoveSpeed * Time.deltaTime, Space.World);
    }
}