using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float changeDirectionInterval = 2f;
    public float arenaSize = 20f;

    private Vector3 moveDirection;
    private float timer;

    void Start()
    {
        PickNewDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= changeDirectionInterval)
        {
            PickNewDirection();
            timer = 0f;
        }

        Vector3 newPos = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -arenaSize / 2, arenaSize / 2);
        newPos.z = Mathf.Clamp(newPos.z, -arenaSize / 2, arenaSize / 2);
        newPos.y = transform.position.y;
        transform.position = newPos;
    }

    void PickNewDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;
    }

    public void ResetPosition(Vector3 position)
    {
        transform.localPosition = position;
    }
}