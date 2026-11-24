using UnityEngine;

public class Enemy : MonoBehaviour
{ 
    public float speed = 2f; //this is float speed defined the speed for the enemy
    public Transform[] points; //this will flip the enemy so that it will look the correct way

    private int i;
    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime); //this is to tell the displacement

        spriteRenderer.flipX = (transform.position.x - points[i].position.x) < 0f; //formula to do the flip
    }
}
