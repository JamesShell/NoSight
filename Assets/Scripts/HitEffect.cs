using UnityEngine;

public class HitEffect : MonoBehaviour
{
    public float lifeTime = 0.4f; // match your animation length

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
