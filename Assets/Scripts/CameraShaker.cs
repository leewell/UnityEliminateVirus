using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    //初始位置
    private Vector3 initPosition;
    //剩余晃动时常
    private float remainingShakeTime;
    //晃动力度大小
    private float shakeStrength;

    private void Start()
    {
        initPosition = transform.localPosition;
    }

    private void Update()
    {
        if (remainingShakeTime > 0)
        {
            remainingShakeTime -= Time.deltaTime;
            if (remainingShakeTime <= 0)
            {
                transform.localPosition = initPosition;
            }
            else
            {
                transform.localPosition = initPosition + Random.insideUnitSphere * shakeStrength;
            }
        }
    }

    public void SetShakeValue(float time,float strength)
    {
        remainingShakeTime = time;
        shakeStrength = strength;
    }
}