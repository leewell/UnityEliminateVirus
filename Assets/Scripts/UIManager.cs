using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject imgSnipeRifle;
    public GameObject imgTakeDamage;
    public GameObject weaponUIView;
    public GameObject[] weaponViews;
    public Text hpTxt;
    public Text bulletTxt;
    public GameObject imgDead;
    [Header("失败音效")]
    public AudioClip lossClip;

    public static UIManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 打开倍镜
    /// </summary>
    public void OpenOrCloseTelesopicView(bool open = true)
    {
        imgSnipeRifle.SetActive(open);
    }

    public void ShowTakeDamageView()
    {
        imgTakeDamage.SetActive(true);
        CancelInvoke();
        Invoke("HideTakeDamageView", 2);
    }

    public void HideTakeDamageView()
    {
        imgTakeDamage.SetActive(false);
    }

    public void ShowOrHideWeaponView(bool show)
    {
        weaponUIView.SetActive(show);
    }

    public void ChangeWeaponView(int id)
    {
        for (int i = 0; i < weaponViews.Length; i++)
        {
            weaponViews[i].SetActive(false);
        }
        weaponViews[id].SetActive(true);
    }

    /// <summary>
    /// 更新血量值
    /// </summary>
    public void UpdateHPValue(int hpValue)
    {
        hpTxt.text = hpValue.ToString();
    }

    /// <summary>
    /// 更新子弹数量
    /// </summary>
    public void UpdateBulletValue(int currentNum, int totalNum)
    {
        bulletTxt.text = currentNum.ToString() + "/" + totalNum.ToString();
    }

    /// <summary>
    /// 显示死亡
    /// </summary>
    public void ShowDead()
    {
        imgDead.SetActive(true);
        Invoke("PlayLossMusic", 2f);
    }

    private void PlayLossMusic()
    {
        AudioSourceManager.instance.PlaySound(lossClip);
        Invoke("LoadNewGameScene", 3f);
    }

    private void LoadNewGameScene()
    {
        SceneManager.LoadScene(0);
    }
}