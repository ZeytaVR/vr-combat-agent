using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float strafeSpeed = 2.5f;
    public float detectionRange = 15f;
    public float attackRange = 1.5f;
    public float strafeRadius = 2f;
    public float resetIdleDuration = 0.5f;
    public float detectionLostIdleDuration = 1.5f;
    public float strafeDuration = 2f;
    public float arenaSize = 20f;

    private enum State { Idle, Chase, Strafe }
    private State currentState = State.Idle;

    private Transform agent;
    private float stateTimer = 0f;
    private float strafeAngle = 0f;
    private int strafeDirection = 1;

    public Vector3 Velocity { get; private set; }
    public bool IsEngaging => currentState == State.Chase || currentState == State.Strafe;

    void Start()
    {
        agent = GameObject.FindWithTag("Agent").transform;
        EnterIdle(resetIdleDuration);
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;
        float distanceToAgent = Vector3.Distance(transform.position, agent.position);

        switch (currentState)
        {
            case State.Idle:
                Velocity = Vector3.zero;
                if (stateTimer <= 0f)
                {
                    if (distanceToAgent <= detectionRange)
                        EnterChase();
                    else
                        EnterIdle(detectionLostIdleDuration);
                }
                break;

            case State.Chase:
                if (distanceToAgent <= attackRange)
                {
                    EnterStrafe();
                }
                else if (distanceToAgent > detectionRange)
                {
                    EnterIdle(detectionLostIdleDuration);
                }
                else
                {
                    Vector3 direction = (agent.position - transform.position).normalized;
                    MoveInDirection(direction, chaseSpeed);
                }
                break;

            case State.Strafe:
                if (distanceToAgent > attackRange * 1.5f)
                {
                    EnterChase();
                }
                else if (stateTimer <= 0f)
                {
                    strafeDirection = Random.value > 0.5f ? 1 : -1;
                    stateTimer = strafeDuration;
                }
                else
                {
                    strafeAngle += strafeDirection * strafeSpeed * Time.deltaTime;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(strafeAngle), 0,
                        Mathf.Sin(strafeAngle)) * strafeRadius;
                    Vector3 targetPos = agent.position + offset;
                    Vector3 direction = (targetPos - transform.position).normalized;
                    MoveInDirection(direction, strafeSpeed);
                }
                break;
        }
    }

    void MoveInDirection(Vector3 direction, float speed)
    {
        Vector3 newPos = transform.position + direction * speed * Time.deltaTime;
        newPos.x = Mathf.Clamp(newPos.x, -arenaSize / 2, arenaSize / 2);
        newPos.z = Mathf.Clamp(newPos.z, -arenaSize / 2, arenaSize / 2);
        newPos.y = transform.position.y;
        transform.position = newPos;
        Velocity = direction * speed;
    }

    void EnterIdle(float duration)
    {
        currentState = State.Idle;
        stateTimer = duration;
        Velocity = Vector3.zero;
    }

    void EnterChase()
    {
        currentState = State.Chase;
        Velocity = Vector3.zero;
    }

    void EnterStrafe()
    {
        currentState = State.Strafe;
        strafeDirection = Random.value > 0.5f ? 1 : -1;
        stateTimer = strafeDuration;
        Velocity = Vector3.zero;
    }

    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        EnterIdle(resetIdleDuration);
    }
}