using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour
{
    [Header("烟雾的例子特效")]
    public ParticleSystem particleSystem;
    private List<GameObject> enemyList = new List<GameObject>();

    private void OnEnable()
    {
        particleSystem.time = 0;
        particleSystem.Play();
        Invoke("HideSelf", 15);
    }

    private void HideSelf()
    {
        particleSystem.Stop();
        Invoke("HideGameObject", 6);
    }

    private void HideGameObject()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelInvoke();
        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i])
            {
                Enemy enemy = enemyList[i].GetComponentInParent<Enemy>();
                if (enemy)
                {
                    enemy.RecoverRange();
                    enemy.SetSmokeColliderState(true);
                }
            }
        }
        enemyList.Clear();
        StopAllCoroutines();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Enemy")
        {
            if (enemyList.Contains(other.gameObject))
            {
                enemyList.Remove(other.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            if (!enemyList.Contains(other.gameObject))
            {
                enemyList.Add(other.gameObject);
                StartCoroutine(OutSmoke(other));
            }
        }
    }

    IEnumerator OutSmoke(Collider other)
    {
        //进入迷雾迷失状态
        yield return new WaitForSeconds(2);
        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy.enabled)
        {
            enemy.SetRange(2);
            enemy.SetSmokeColliderState(false);
        }
        //解除迷雾迷失状态
        yield return new WaitForSeconds(15);
        if (enemy.enabled)
        {
            enemy.SetSmokeColliderState(true);
        }
    }
}