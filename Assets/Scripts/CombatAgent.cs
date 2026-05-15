using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CombatAgent : Agent
{
    public Transform enemy;
    public float moveSpeed = 3f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.5f;

    private Rigidbody rb;
    private float agentHealth = 100f;
    private float enemyHealth = 100f;
    private float attackTimer = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(-2f, 1f, 0f);
        rb.velocity = Vector3.zero;
        enemy.GetComponent<EnemyMovement>().ResetPosition(new Vector3(2f, 1f, 0f));
        agentHealth = 100f;
        enemyHealth = 100f;
        attackTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(enemy.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, enemy.localPosition));
        sensor.AddObservation(agentHealth / 100f);
        sensor.AddObservation(enemyHealth / 100f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
    attackTimer -= Time.fixedDeltaTime;

    float moveX = actions.ContinuousActions[0];
    float moveZ = actions.ContinuousActions[1];
    Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed;
    rb.AddForce(move, ForceMode.VelocityChange);

    float distanceToEnemy = Vector3.Distance(transform.localPosition, enemy.localPosition);

    // Reward for closing distance
    AddReward(0.01f * (1f / (distanceToEnemy + 0.1f)));

    // Penalty for standing still in attack range without attacking
    float speed = rb.velocity.magnitude;
    if (distanceToEnemy < attackRange && attackTimer > 0f && speed < 0.5f)
    {
        AddReward(-0.01f);
    }

    bool attack = actions.DiscreteActions[0] == 1;

    if (attack && distanceToEnemy < attackRange && attackTimer <= 0f)
    {
        enemyHealth -= 25f;
        attackTimer = attackCooldown;
        AddReward(0.3f);

        if (enemyHealth <= 0f)
        {
            AddReward(1.0f);
            EndEpisode();
            return;
        }
    }

    // Enemy deals damage back periodically
    if (distanceToEnemy < attackRange)
    {
        agentHealth -= 0.5f;
    }

    if (agentHealth <= 0f)
    {
        AddReward(-1.0f);
        EndEpisode();
        return;
    }

    if (transform.localPosition.y < 0)
    {
        AddReward(-1.0f);
        EndEpisode();
        return;
    }

    AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }
}