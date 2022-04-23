using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoctorController : MonoBehaviour
{
    //重力加速度
    private const int gravite = 10;

    private CharacterController characterController;
    [Header("玩家初始生命值")]
    public int initHP = 100;
    [Header("主角的移动速度")]
    public float moveSpeed = 5f;
    [Header("玩家奔跑速度")]
    public float runSpeed = 7f;
    [Header("鼠标转向灵敏度")]
    public float sensitivity = 2.4f;
    [Header("武器重量导致的移动减速")]
    public float decreaseSpeed;
    //实际移动速度
    public float actualSpeed;
    //跳高速度
    private float jumpSpeed = 0f;
    private float hor;
    private float ver;
    //移动向量
    private Vector3 dir;
    //主角是否在地面上
    private bool isGround = true;
    private CollisionFlags collisionFlags;

    //记录上一帧的转向欧拉角
    private float angleX;
    private float angleY;

    //当前武器编号id
    private int currentWeaponID = -1;
    private Transform weaponPlaceTrans;
    //武器字典(key-武器ID)
    private Dictionary<int,Weapon> weaponDic = new Dictionary<int, Weapon>();
    //武器库(key-物品id，value-数量)  玩家的武器库，相当于背包，当前玩家某个武器以及子弹的剩余数量
    private Dictionary<int, int> ammoInventory = new Dictionary<int, int>();

    private Transform cameraTrans;
    //当前生命值
    private int currentHP;
    public CameraShaker cameraShaker;
    //是否死亡
    public bool dead;
    [Header("死亡位置")]
    public Transform deadPositionTrans;
    [Header("跳起音效")]
    public AudioClip jumpClip;
    [Header("死亡音效")]
    public AudioClip deadClip;
    [Header("受伤音效")]
    public AudioClip hurtClip;
    [Header("着陆音效")]
    public AudioClip landClip;
    //是否能播放着陆音效
    private bool canPlayLandClip;

    public Weapon[] weapons;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        angleY = transform.eulerAngles.y;
        cameraTrans = Camera.main.transform;
        angleX = cameraTrans.eulerAngles.x;

        weaponPlaceTrans = cameraTrans.Find("WeapomPlace");
    }

    private void Start()
    {
        //设置鼠标隐藏并且锁定到Game视图下
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        currentHP = initHP;
        UIManager.instance.ShowOrHideWeaponView(false);
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(false);
            weapons[i].PickUp(this);
        }
    }

    private void Update()
    {
        if (dead)
        {
            return;
        }
        Move();
        TurnAndLook();
        Jump();
        ChangeCurrentWeapon();
    }

    #region 角色移动相关功能
    /// <summary>
    /// 角色移动
    /// </summary>
    private void Move()
    {
        actualSpeed = Input.GetButton("Run") ? runSpeed - decreaseSpeed : moveSpeed - decreaseSpeed;
        hor = Input.GetAxis("Horizontal");
        ver = Input.GetAxis("Vertical");
        dir = new Vector3(hor, 0, ver);
        collisionFlags = characterController.Move(transform.TransformDirection(dir.normalized * actualSpeed * Time.deltaTime));
        if (hor <= 0.1f && ver <= 0.1f)
        {
            actualSpeed = 0;
        }
    }

    /// <summary>
    /// 角色转向和看
    /// </summary>
    private void TurnAndLook()
    {
        angleY += Input.GetAxis("Mouse X") * sensitivity;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, angleY, transform.eulerAngles.z);
        float lookAngle = -Input.GetAxis("Mouse Y") * sensitivity;
        angleX = Mathf.Clamp(angleX + lookAngle, -60f, 60f);
        cameraTrans.eulerAngles = new Vector3(angleX, cameraTrans.eulerAngles.y, cameraTrans.eulerAngles.z);
    }

    /// <summary>
    /// 角色跳跃
    /// </summary>
    private void Jump()
    {
        if (Input.GetButton("Jump") && isGround)
        {
            isGround = false;
            jumpSpeed = 5;
            canPlayLandClip = true;
            AudioSourceManager.instance.PlaySound(jumpClip, 0.8f, 1.1f);
        }
        if (!isGround) // 跳起来了
        {
            jumpSpeed = jumpSpeed - gravite * Time.deltaTime;
            Vector3 jump = new Vector3(0, jumpSpeed * Time.deltaTime, 0);
            collisionFlags = characterController.Move(jump);
            if (collisionFlags == CollisionFlags.Below)
            {
                jumpSpeed = 0;
                isGround = true;
            }
        }

        if (isGround && collisionFlags == CollisionFlags.None)
        {
            if (canPlayLandClip)
            {
                canPlayLandClip = false;
                AudioSourceManager.instance.PlaySound(landClip, 0.8f, 1.1f);
            }
            isGround = false;
        }
    }
    #endregion

    /// <summary>
    /// 具体切换武器
    /// </summary>
    private void ChangeWeapon(int id)
    {
        if (weaponDic.Count == 0)
        {
            return;
        }
        if (id > weaponDic.Keys.Max())
        {
            id = weaponDic.Keys.Min();
        }else if (id < weaponDic.Keys.Min())
        {
            id = weaponDic.Keys.Max();
        }
        if (id == currentWeaponID) // 如果只有1种武器的时候，就不允许切换，否则会出现颠簸颤抖的情况
        {
            return;
        }
        //循环处理当捡起的武器id跨度较大时，切换武器报错的问题
        while (!weaponDic.ContainsKey(id))
        {
            if (id > currentWeaponID) // 滑轮向上滑，即往大的id滑动
            {
                id++;
            }else // 向下滑
            {
                id--;
            }
        }

        if (currentWeaponID != -1) // 排除第一次没有武器的情况
        {
            //隐藏上一把武器
            weaponDic[currentWeaponID].PutAway();
        }
        //显示当前武器
        weaponDic[id].Selected();
        currentWeaponID = id;
    }

    /// <summary>
    /// 切换当前武器
    /// </summary>
    /// <param name="autoChange">是否可以自动切枪</param>
    public void ChangeCurrentWeapon(bool autoChange = false)
    {
        if (autoChange) // 如果可以自动切枪，则默认切换到最新的一把枪
        {
            //ChangeWeapon(weaponDic.Count - 1);
            ChangeWeapon(weaponDic.Keys.Last());
        }
        else
        {
            //通过鼠标滑轮切换武器
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                ChangeWeapon(currentWeaponID + 1);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                ChangeWeapon(currentWeaponID - 1);
            }
            //处理字母上面的数字按键切换武器
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    int num;
                    if (i == 0)
                    {
                        num = 10; // 按键0对应第10把武器
                    }
                    else
                    {
                        num = i - 1; // 其他的按键依次-1，因为下标是从0开始
                    }
                    if (weaponDic.ContainsKey(num))
                    {
                        ChangeWeapon(num);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 捡起武器
    /// </summary>
    public void PickUpWeapon(int weaponID)
    {
        if (weaponDic.ContainsKey(weaponID)) // 如果武器列表里存在这种武器
        {
            //补充弹药
            Weapon weapon = weaponDic[weaponID];
            ammoInventory[weapon.GetItemID()] = weapon.GetInitAmout();
            weapon.SetClipContent(weapon.clipSize);
            if (currentWeaponID == weaponID) // 捡起同一把武器，修改子弹UI
            {
                UIManager.instance.UpdateBulletValue(weapon.clipSize, weapon.GetInitAmout());
            }
        }
        else // 如果这种武器不在武器列表里
        {
            //生成
            //GameObject weaponGo = Instantiate(Resources.Load<GameObject>("Prefabs/Weapons/" + weaponID));
            //weaponGo.transform.SetParent(weaponPlaceTrans);
            //weaponGo.transform.localPosition = Vector3.zero;
            //weaponGo.transform.localRotation = Quaternion.identity;
            //weaponGo.gameObject.SetActive(false);
            //Weapon weapon = weaponGo.GetComponent<Weapon>();
            //weapon.PickUp(this);
            weapons[weaponID].SetClipContent(weapons[weaponID].clipSize); // 把弹夹里的子弹装满 
            weaponDic.Add(weaponID, weapons[weaponID]);
            ammoInventory.Add(weaponID, weapons[weaponID].GetInitAmout());
            ChangeWeapon(weaponID);
        }
    }

    /// <summary>
    /// 获取某个武器在武器库中子弹的剩余数量
    /// </summary>
    /// <param name="id"></param>
    public int GetAmmoAmout(int id)
    {
        ammoInventory.TryGetValue(id, out int value);
        return value;
    }

    /// <summary>
    /// 更新武器库中某个武器的剩余子弹数量
    /// </summary>
    /// <param name="id"></param>
    /// <param name="value"></param>
    public void UpdateAmmoAmout(int id,int value)
    {
        if (ammoInventory.ContainsKey(id))
        {
            ammoInventory[id] += value;
        }
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    /// <param name="value">伤害值：如果value>0 是减血，如果value<0 是加血</param>
    public void TakeDamage(int value)
    {
        if (dead)
        {
            return;
        }
        if (value < 0) //加血
        {
            if (currentHP < initHP) // 是否已经超过最大血量
            {
                currentHP -= value;
            }
        }
        else // 减血
        {
            currentHP -= value;
        }
        
        if (currentHP <= 0)
        {
            dead = true;
            cameraTrans.localPosition = deadPositionTrans.localPosition;
            cameraTrans.eulerAngles = deadPositionTrans.eulerAngles;
            weaponPlaceTrans.gameObject.SetActive(false);
            currentHP = 0;
            AudioSourceManager.instance.PlaySound(deadClip);
            UIManager.instance.ShowDead();
        }
        else
        {
            if (value > 0)
            {
                AudioSourceManager.instance.PlaySound(hurtClip);
                UIManager.instance.ShowTakeDamageView();
                cameraShaker.SetShakeValue(0.2f, 0.5f);
            }
        }
        UIManager.instance.UpdateHPValue(currentHP);
    }
}