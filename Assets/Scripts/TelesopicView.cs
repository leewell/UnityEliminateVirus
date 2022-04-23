using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelesopicView : MonoBehaviour
{
    [Header("打开/关闭倍镜的速度")]
    public int zoomSpeed = 100;
    [Header("倍镜等级(几倍镜)")]
    public int zoomLevel = 2;
    //初始视野距离
    private float initFov;
    //是否打开了倍镜
    [HideInInspector]
    public bool openTelesopicView = false;
    
    private void Start()
    {
        initFov = Camera.main.fieldOfView;
    }

    private void Update()
    {
        if (openTelesopicView)
        {
            OpenTelesopicView();
        }
        else
        {
            CloseTelesopicView();
        }
    }

    /// <summary>
    /// 打开倍镜
    /// </summary>
    private void OpenTelesopicView()
    {
        if (Camera.main.fieldOfView != initFov / zoomLevel)
        {
            if (Mathf.Abs(Camera.main.fieldOfView - initFov / zoomLevel) < 5f)
            {
                Camera.main.fieldOfView = initFov / zoomLevel;
            }
            else
            {
                Camera.main.fieldOfView -= zoomSpeed * Time.deltaTime;
            }
        }
        UIManager.instance.OpenOrCloseTelesopicView(true);
    }

    /// <summary>
    /// 关闭倍镜
    /// </summary>
    private void CloseTelesopicView()
    {
        if (Camera.main.fieldOfView != initFov)
        {
            if (Mathf.Abs(Camera.main.fieldOfView - initFov) < 5f)
            {
                Camera.main.fieldOfView = initFov;
            }
            else
            {
                Camera.main.fieldOfView += zoomSpeed * Time.deltaTime;
            }
        }
        UIManager.instance.OpenOrCloseTelesopicView(false);
    }

    public void OpenTheTelesopicView(bool open = true)
    {
        openTelesopicView = open;
    }
}