using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject keyGo;
    [Header("门的移动速度")]
    public float moveSpeed = 5f;
    //是否已经解锁了
    private bool unlocked = false;
    public AudioClip openDoorClip;
    public AudioClip dontOpenClip;
    public AudioClip doorClip;

    private void Update()
    {
        if (unlocked)
        {
            transform.Translate(Vector3.down * Time.deltaTime * moveSpeed);
            if (transform.position.y < -1.4f)
            {
                Destroy(gameObject);
            }
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (!unlocked)
            {
                if (keyGo == null)
                {
                    AudioSourceManager.instance.PlaySound(openDoorClip);
                    AudioSourceManager.instance.PlaySound(doorClip);
                    unlocked = true;
                }
                else
                {
                    AudioSourceManager.instance.PlaySound(dontOpenClip);
                }
            }
        }
    }
}