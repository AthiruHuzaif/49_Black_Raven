using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public Transform[] points;

    private int i;
    private SpriteRenderer spriteRenderer;
    void Start()
    {

    }

    private void Update()
    {
        if (Vector2.Distance(transform.position, points[i].position) < 0.25f)
        {
            i++;
            if (i == points.Length)
            {
                i = 0;
            }
        }

        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);

        bool isIdle = (transform.position.x - points[i].position.x) < 0f;
        spriteRenderer.flipX = isIdle;
    }
}
