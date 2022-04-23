using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBehavior : MonoBehaviour
{
    //boss武器的显示（初始时显示，当boss手臂挥舞到胸前时隐藏，然后生成出下面的skillWeapon）
    public GameObject bossWeaponViewGo;
    private Transform playerTrans;
    //用于对玩家进行真正攻击的技能游戏物体
    public GameObject skillWeapon;
    //技能球发射点
    public Transform attackTrans;
    public AudioClip skillClip;

    private void Awake()
    {
        playerTrans = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        if (skillWeapon)
        {
            PoolManager.Instance.InitPool(skillWeapon, 10);
        }
    }

    /// <summary>
    /// 动画帧事件，显示攻击武器
    /// </summary>
    private void ShowWeapon()
    {
        bossWeaponViewGo.SetActive(true);
    }

    /// <summary>
    /// 动画帧事件，隐藏攻击武器
    /// </summary>
    private void HideWeapon()
    {
        bossWeaponViewGo.SetActive(false);
    }

    /// <summary>
    /// 动画帧事件，创造攻击技能武器
    /// </summary>
    private void CreateSkillBall()
    {
        bossWeaponViewGo.SetActive(false);
        GameObject go = PoolManager.Instance.GetInstance<GameObject>(skillWeapon);
        go.transform.SetParent(null);
        go.SetActive(true);
        go.transform.position = attackTrans.position;
        go.transform.LookAt(playerTrans);
        transform.parent.LookAt(new Vector3(playerTrans.position.x, transform.position.y, playerTrans.position.z));
        AudioSourceManager.instance.PlaySound(skillClip);
    }
}