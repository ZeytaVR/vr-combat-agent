using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ThunderRoad;
using Unity.Barracuda;

namespace MLCombatAgent
{
    public class MLCombatBrain : ThunderBehaviour
    {
        [Header("ML Model")]
        public NNModel modelAsset;

        private Creature creature;
        private NavMeshAgent navAgent;
        private Model runtimeModel;
        private IWorker worker;

        // LSTM hidden state (h and c combined, size 128)
        private Tensor recurrentIn;

        // Decision timing
        private float decisionTimer = 0f;
        private const float DecisionInterval = 0.2f;

        // Attack cooldown
        private float attackTimer = 0f;
        private const float AttackCooldown = 0.5f;
        private const float AttackRange = 1.5f;

        // Observation normalisation constants
        private const float MaxRelativePos = 25f;
        private const float MaxSpeed = 5f;
        private const float MaxArenaDistance = 35f;
        private const float MaxEnemySpeed = 2f;

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

        protected void Start()
        {
            creature = GetComponentInParent<Creature>();
            if (creature == null)
            {
                Debug.LogError("[MLCombatBrain] No Creature component found.");
                return;
            }

            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogError("[MLCombatBrain] No NavMeshAgent found on Brain.");
                return;
            }

            // Only initialise from asset if provided (SDK testing mode)
            if (modelAsset != null)
            {
                runtimeModel = ModelLoader.Load(modelAsset);
                worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
                recurrentIn = new Tensor(1, 1, 128, 1);
                Debug.Log("[MLCombatBrain] Initialised from asset.");
            }
        }

        public void LoadModelFromPath(string path)
        {
            byte[] modelData = System.IO.File.ReadAllBytes(path);
            runtimeModel = ModelLoader.Load(modelData);
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
            recurrentIn = new Tensor(1, 1, 128, 1);
            Debug.Log("[MLCombatBrain] Model loaded from path successfully.");
        }

        protected override void ManagedUpdate()
        {
            if (worker == null || creature == null) return;

            decisionTimer += Time.deltaTime;
            attackTimer -= Time.deltaTime;

            if (decisionTimer < DecisionInterval) return;
            decisionTimer = 0f;

            // Find player
            Creature player = Creature.allActive.Find(c => c.isPlayer);
            if (player == null) return;

            // Build observation vector
            float[] obs = BuildObservations(creature, player);

            // Run ONNX inference
            RunInference(obs, out float moveX, out float moveZ, out bool attack);

            // Apply movement
            ApplyMovement(moveX, moveZ);

            // Apply attack
            if (attack && attackTimer <= 0f)
            {
                float distance = Vector3.Distance(creature.transform.position, player.transform.position);
                if (distance < AttackRange)
                {
                    TriggerAttack();
                    attackTimer = AttackCooldown;
                }
            }
        }

        private float[] BuildObservations(Creature npc, Creature player)
        {
            Vector3 npcPos = npc.transform.position;
            Vector3 playerPos = player.transform.position;
            Vector3 relativePos = playerPos - npcPos;
            float distance = relativePos.magnitude;
            Vector3 dir = distance > 0.01f ? relativePos.normalized : Vector3.zero;
            Vector3 npcVel = npc.locomotion.velocity;
            Vector3 playerVel = player.locomotion.velocity;

            return new float[]
            {
                relativePos.x / MaxRelativePos,
                relativePos.y / 5f,
                relativePos.z / MaxRelativePos,
                npcVel.x / MaxSpeed,
                npcVel.y / MaxSpeed,
                npcVel.z / MaxSpeed,
                distance / MaxArenaDistance,
                dir.x,
                dir.z,
                1f,   // NPC health placeholder
                1f,   // Player health placeholder
                playerVel.magnitude / MaxEnemySpeed
            };
        }

        private void RunInference(float[] obs, out float moveX, out float moveZ, out bool attack)
        {
            moveX = 0f;
            moveZ = 0f;
            attack = false;

            var inputTensor = new Tensor(1, 1, 1, 12, obs);
            var actionMasks = new Tensor(1, 1, 1, 2, new float[] { 1f, 1f });

            var inputs = new Dictionary<string, Tensor>
            {
                { "obs_0", inputTensor },
                { "action_masks", actionMasks },
                { "recurrent_in", recurrentIn }
            };

            worker.Execute(inputs);

            Tensor actionOut = worker.PeekOutput("deterministic_continuous_actions");
            moveX = Mathf.Clamp(actionOut[0], -1f, 1f);
            moveZ = Mathf.Clamp(actionOut[1], -1f, 1f);

            Tensor discreteOut = worker.PeekOutput("deterministic_discrete_actions");
            attack = discreteOut[0] > 0.5f;

            Tensor newRecurrent = worker.PeekOutput("recurrent_out");
            recurrentIn.Dispose();
            recurrentIn = new Tensor(newRecurrent.shape, newRecurrent.ToReadOnlyArray());

            inputTensor.Dispose();
            actionMasks.Dispose();
        }

        private void ApplyMovement(float moveX, float moveZ)
        {
            Vector3 direction = new Vector3(moveX, 0f, moveZ);
            if (direction.magnitude > 1f) direction = direction.normalized;

            Vector3 targetPos = creature.transform.position + direction * 2f;
            navAgent.SetDestination(targetPos);
        }

        private void TriggerAttack()
        {
            Debug.Log("[MLCombatBrain] Attack triggered.");
        }

        protected void OnDestroy()
        {
            recurrentIn?.Dispose();
            worker?.Dispose();
        }
    }
}