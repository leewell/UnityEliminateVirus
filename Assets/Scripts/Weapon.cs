using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    private DoctorController owner;
    [Header("武器对应的id")]
    public int itemID = -1;
    [Header("初始数量:如果是手雷为1，枪就是子弹的数量")]
    public int initAmount = -1;
    [Header("子弹的光线拖尾")]
    public LineRenderer rayTrailPrefabs;
    [Header("子弹发射力")]
    public float projectileLuanchForce = 200f;
    [Header("武器减速值")]
    public float decreaseSpeed;
    //子弹的发射点
    public Transform shootPoint;
    private Animator animator;

    public WeaponType weaponType = WeaponType.Raycast;
    private List<ActiveTrail> activeTrails = new List<ActiveTrail>();

    public Projectile projectilePrefab;
    public AdvancedWeaponSetting advancedWeaponSetting;
    [Header("武器的攻击模式")]
    public WeaponMode weaponMode = WeaponMode.Normal;
    [Header("弹夹里可以装填子弹的最大数量")]
    public int clipSize = 4;
    //当前弹夹里的剩余子弹数量
    private int clipContent;
    private WeaponState currentWeaponState = WeaponState.Idle;
    private int fireNameHash = Animator.StringToHash("fire");
    private int reloadNameHash = Animator.StringToHash("reload");
    //更换弹夹的速度
    public float reloadTime = 2.0f;
    //攻击频率(CD时间)
    public float fireRate = 0.5f;
    //cd的timer
    private float shootTimer;
    public AnimationClip fireAnimationClip;
    public AnimationClip reloadAnimationClip;
    [Header("枪的伤害值")]
    public float damageValue = 2;
    [Header("是否有倍镜")]
    public bool hasTelesopicView = false;
    private TelesopicView telesopicView;
    //激光射线打中敌人时的粒子特效
    public ParticleSystem raycastHitEffectPrefab;
    private int bulletSize;
    public ParticleSystem bulletViewEffect;
    private float currentDamageValue;
    [Header("射击音效")]
    public AudioClip shootClip;
    [Header("打到墙的音效")]
    public AudioClip hitWallClip;
    [Header("换子弹音效")]
    public AudioClip reloadClip;
    [Header("没有子弹音效")]
    public AudioClip cockClip;
    private float chargeTimer = 0;
    public AudioSource chargeAudioSource;
    public AudioSource cockAudioSource;


    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (rayTrailPrefabs != null) // 如果当前武器可以发射子弹
        {
            //需要先生成几个备用的预制体
            PoolManager.Instance.InitPool(rayTrailPrefabs, 8);
        }
        if (projectilePrefab != null)  // 如果当前武器可以发射子弹
        {
            PoolManager.Instance.InitPool(projectilePrefab, 4);
        }
        if (raycastHitEffectPrefab)
        {
            PoolManager.Instance.InitPool(raycastHitEffectPrefab, 8);
        }
        clipContent = clipSize;
        if (hasTelesopicView)
        {
            telesopicView = Camera.main.GetComponent<TelesopicView>();
        }
    }


    private void Update()
    {
        UpdateWeaponStateController();
        FireInput();
        if (Input.GetButtonDown("Reload"))
        {
            Reload();
        }
        if (shootTimer > 0)
        {
            shootTimer -= Time.deltaTime;
        }
        UpdateTrailState();
    }

    /// <summary>
    /// 获取武器id
    /// </summary>
    public int GetItemID()
    {
        return itemID;
    }

    /// <summary>
    /// 获取手雷/子弹数量
    /// </summary>
    public int GetInitAmout()
    {
        return initAmount;
    }

    /// <summary>
    /// 选择当前武器
    /// </summary>
    public void Selected()
    {
        gameObject.SetActive(clipContent != 0 || owner.GetAmmoAmout(itemID) != 0);
        animator.SetTrigger("selected");
        if (fireAnimationClip != null)
        {
            animator.SetFloat("fireTime", fireAnimationClip.length / fireRate);
        }
        if (reloadAnimationClip != null)
        {
            animator.SetFloat("reloadTime", reloadAnimationClip.length / reloadTime);
        }
        owner.decreaseSpeed = decreaseSpeed;
        UIManager.instance.ShowOrHideWeaponView(true);
        UIManager.instance.ChangeWeaponView(itemID);
        UIManager.instance.UpdateBulletValue(clipContent, owner.GetAmmoAmout(itemID));
        if (reloadClip)
        {
            AudioSourceManager.instance.PlaySound(reloadClip);
        }
    }

    /// <summary>
    /// 捡起武器，指定当前武器的拥有者即DoctorController的引用
    /// </summary>
    /// <param name="controller"></param>
    public void PickUp(DoctorController controller)
    {
        owner = controller;
    }

    /// <summary>
    /// 收起武器
    /// </summary>
    public void PutAway()
    {
        gameObject.SetActive(false);
        if (weaponMode == WeaponMode.Accumulation)
        {
            InitAccumulateWeapon();
        }
        if (weaponType == WeaponType.Raycast)
        {
            for (int i = 0; i < activeTrails.Count; i++)
            {
                activeTrails[i].renderer.gameObject.SetActive(false);
            }
            activeTrails.Clear();
        }
    }

    /// <summary>
    /// 开枪
    /// </summary>
    private void Fire()
    {
        if (currentWeaponState != WeaponState.Idle || shootTimer > 0)
        {
            return;
        }
        if (clipContent == 0)
        {
            if (!cockAudioSource.isPlaying)
            {
                cockAudioSource.Play();
            }
            return;
        }
        shootTimer = fireRate;
        animator.SetTrigger("fire");
        clipContent--;
        UIManager.instance.UpdateBulletValue(clipContent, owner.GetAmmoAmout(itemID));
        AudioSourceManager.instance.PlaySound(shootClip);
        currentWeaponState = WeaponState.Firing;
        owner.cameraShaker.SetShakeValue(advancedWeaponSetting.shakeTime, advancedWeaponSetting.shakeStrength * 0.05f);
        if (weaponType == WeaponType.Raycast)
        {
            RayCastShoot();
        }
        else
        {
            ProjectileShoot();
        }
    }

    /// <summary>
    /// 发射镭射类型枪的攻击方式
    /// </summary>
    private void RayCastShoot()
    {
        //发散比例(单位长度)
        float spreatRadio = advancedWeaponSetting.spreatAngle / Camera.main.fieldOfView;
        Vector2 spreat = spreatRadio * UnityEngine.Random.insideUnitCircle;
        Ray ray = Camera.main.ViewportPointToRay(Vector3.one * 0.5f + (Vector3)spreat);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            ParticleSystem p = PoolManager.Instance.GetInstance<ParticleSystem>(raycastHitEffectPrefab);
            p.transform.position = hit.point;
            p.transform.forward = hit.normal;
            p.gameObject.SetActive(true);
            p.Play();
            if (hit.collider.gameObject.layer == 9) // 打到敌人
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                enemy.TakeDamage(damageValue);
            }else if (hit.collider.gameObject.layer == 10) // 打到boss
            {
                Boss boss = hit.collider.GetComponentInParent<Boss>();
                boss.TakeDamage(damageValue, transform.position);
            }
            else // 默认打到墙
            {
                AudioSourceManager.instance.PlaySound(hitWallClip);
            }

            if (rayTrailPrefabs != null) // 安全校验
            {
                LineRenderer lineRenderer = PoolManager.Instance.GetInstance<LineRenderer>(rayTrailPrefabs);
                lineRenderer.gameObject.SetActive(true);
                Vector3[] trailPos = new Vector3[] {shootPoint.position,hit.point };
                lineRenderer.SetPositions(trailPos);
                activeTrails.Add(new ActiveTrail
                {
                    renderer = lineRenderer,
                    direction = (trailPos[1] - trailPos[0]).normalized,
                    remainningTime = 0.3f
                }) ;
            }
        }
    }

    /// <summary>
    /// 更新激光拖尾效果（模拟位移）
    /// </summary>
    private void UpdateTrailState()
    {
        Vector3[] pos = new Vector3[2];
        for (int i = 0; i < activeTrails.Count; i++)
        {
            ActiveTrail trail = activeTrails[i];
            trail.renderer.GetPositions(pos);
            trail.remainningTime -= Time.deltaTime;
            pos[0] += trail.direction * 50 * Time.deltaTime;
            //pos[1] += trail.direction * 50 * Time.deltaTime; // 防止射线穿透
            trail.renderer.SetPositions(pos);
            if (trail.remainningTime <= 0 || Vector3.Distance(pos[0],pos[1]) < 0.5f)
            {
                trail.renderer.gameObject.SetActive(false);
                activeTrails.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// 发射子弹类型枪的攻击方式（包括投掷类武器）
    /// </summary>
    private void ProjectileShoot()
    {
        Projectile projectile = PoolManager.Instance.GetInstance<Projectile>(projectilePrefab);
        projectile.gameObject.SetActive(true);
        if (weaponMode == WeaponMode.Accumulation)
        {
            projectile.SetBulletDamageValue(currentDamageValue);
            projectile.SetBulletSize(bulletSize);

        }
        //使用发散会有问题
        //Vector2 spreatDir = Random.insideUnitCircle * Mathf.Sign(advancedWeaponSetting.spreatAngle * Mathf.Deg2Rad); // 发散方向
        Vector2 spreatDir = Vector3.forward * Mathf.Sign(advancedWeaponSetting.spreatAngle * Mathf.Deg2Rad); // 发散方向
        Vector3 dir = shootPoint.forward + (Vector3)spreatDir;
        dir.Normalize();
        projectile.Launch(this, dir, projectileLuanchForce);
    }

    public Transform GetShootPoint()
    {
        return shootPoint;
    }


    /// <summary>
    /// 更换弹夹
    /// </summary>
    public void Reload()
    {
        if (clipSize == clipContent || currentWeaponState != WeaponState.Idle) // 说明弹夹是满的，无需更换
        {
            return;
        }
        int remainingBullet = owner.GetAmmoAmout(itemID); // 获取这把武器的所有子弹
        if (remainingBullet == 0) // 如果没有子弹了
        {
            if (itemID == 2 || itemID == 6) // 如果投掷类武器没有子弹，需要隐藏
            {
                PutAway();
            }
            return;
        }
        currentWeaponState = WeaponState.Reloading;
        //获取可以更换的子弹数量
        int chargeInClip = Mathf.Min(remainingBullet, clipSize - clipContent);
        clipContent += chargeInClip;
        owner.UpdateAmmoAmout(itemID, -chargeInClip);
        UIManager.instance.UpdateBulletValue(clipContent, owner.GetAmmoAmout(itemID));
        animator.SetTrigger("reload");
        if (weaponMode == WeaponMode.Accumulation) // 如果是聚能枪的话，换弹夹需要隐藏聚能特效
        {
            bulletViewEffect.gameObject.SetActive(false);
            bulletViewEffect.startSize = 0;
        }
        if (reloadClip)
        {
            AudioSourceManager.instance.PlaySound(reloadClip);
        }
    }

    /// <summary>
    /// 更新武器状态
    /// </summary>
    private void UpdateWeaponStateController()
    {
        animator.SetFloat("moveSpeed", owner.actualSpeed / 5);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        WeaponState newWeaponState;
        if (stateInfo.shortNameHash == fireNameHash)
        {
            newWeaponState = WeaponState.Firing;
        }else if (stateInfo.shortNameHash == reloadNameHash)
        {
            newWeaponState = WeaponState.Reloading;
        }
        else
        {
            newWeaponState = WeaponState.Idle;
        }
        if (newWeaponState != currentWeaponState)
        {
            WeaponState lastState = currentWeaponState;
            currentWeaponState = newWeaponState;
            if (lastState == WeaponState.Firing && clipContent == 0) // 如果子弹打完，再次按下攻击键的话，就自动更换弹夹
            {
                Reload();
            }
        }
    }

    public bool HasBullet()
    {
        return clipContent > 0;
    }

    private void FireInput()
    {
        switch (weaponMode)
        {
            case WeaponMode.Normal:
                if (Input.GetMouseButtonDown(0))
                {
                    Fire();
                }
                if (hasTelesopicView)
                {
                    if (Input.GetMouseButton(1))
                    {
                        telesopicView.OpenTheTelesopicView(true);
                    }
                    else
                    {
                        telesopicView.OpenTheTelesopicView(false);
                    }
                    if (currentWeaponState == WeaponState.Reloading)
                    {
                        telesopicView.OpenTheTelesopicView(false);
                    }
                }
                break;
            case WeaponMode.Auto:
                if (Input.GetMouseButton(0))
                {
                    Fire();
                }
                break;
            case WeaponMode.Accumulation:
                if (Input.GetMouseButtonUp(0))
                {
                    Fire();
                    InitAccumulateWeapon();
                }
                else if (Input.GetMouseButton(0) && clipContent > 0 && currentWeaponState != WeaponState.Reloading)
                {
                    AccumulateEnergy();
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 初始化聚能枪
    /// </summary>
    private void InitAccumulateWeapon()
    {
        chargeTimer = 0;
        chargeAudioSource.Stop();
        bulletViewEffect.gameObject.SetActive(false);
        bulletViewEffect.startSize = 0;
    }

    /// <summary>
    /// 聚能
    /// </summary>
    public void AccumulateEnergy()
    {
        bulletViewEffect.gameObject.SetActive(true);
        if (bulletViewEffect.startSize <= 0.3f)
        {
            bulletViewEffect.startSize += Time.deltaTime;
        }
        if (currentDamageValue <= damageValue * 5)
        {
            currentDamageValue += Time.deltaTime;
        }
        //第一次播放完整的聚能音效
        if (chargeTimer <= 0)
        {
            chargeTimer = Time.time;
            chargeAudioSource.time = 0;
            chargeAudioSource.Play();
        }
        //播放持续聚能的音效循环部分(即聚能)
        if (Time.time - chargeTimer >= 1.463f)
        {
            if (!chargeAudioSource.isPlaying)
            {
                //只播放某一个时间点到最后的音效
                chargeAudioSource.time = 1.3f;
                chargeAudioSource.Play();

            }
        }
    }

    /// <summary>
    /// 重新捡起同一把武器，需要把弹夹装满
    /// </summary>
    /// <param name="count"></param>
    public void SetClipContent(int count)
    {
        clipContent = count;
    }
}

/// <summary>
/// 武器发射子弹的类型
/// </summary>
public enum WeaponType
{
    Raycast, // 射线射击
    Projectile // 子弹射击
}

/// <summary>
/// 武器状态
/// </summary>
public enum WeaponState
{
    Idle,Firing,Reloading
}

/// <summary>
/// 武器模式
/// </summary>
public enum WeaponMode
{
    Normal, // 普通单点
    Auto, //全自动枪
    Accumulation // 聚力枪
}

/// <summary>
/// 激光信息类
/// </summary>
public class ActiveTrail
{
    public LineRenderer renderer;
    public Vector3 direction; // 方向
    public float remainningTime; // 剩余时间
}

/// <summary>
/// 武器的额外设置
/// </summary>
[System.Serializable]
public class AdvancedWeaponSetting
{
    //偏移（发散）角度，单位不是传统意义上的度数，而是计量单位
    [Header("子弹偏移角度")]
    public float spreatAngle;
    [Header("震动时间")]
    public float shakeTime;
    [Header("震动力度")]
    public float shakeStrength;
}