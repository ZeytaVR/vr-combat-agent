using ThunderRoad;
using UnityEngine;
using System.IO;
using System.Collections;

namespace MLCombatAgent
{
    public class MLCombatAgentMod : MonoBehaviour
    {
        private const string ModelFileName = "combat-agent-v7.onnx";
        private string modelPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            GameObject go = new GameObject("MLCombatAgentMod");
            go.AddComponent<MLCombatAgentMod>();
            DontDestroyOnLoad(go);
            Debug.Log("[MLCombatAgent] Mod entry point created.");
        }

        void Start()
        {
            modelPath = Path.Combine(
                Application.streamingAssetsPath,
                "Mods", "MLCombatAgent", ModelFileName);

            StartCoroutine(PollForCreatures());
            Debug.Log("[MLCombatAgent] Polling for creatures.");
        }

        IEnumerator PollForCreatures()
        {
            while (true)
            {
                foreach (Creature creature in Creature.allActive)
                {
                    TryAttachBrain(creature);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        void TryAttachBrain(Creature creature)
        {
            if (creature.isPlayer) return;
            if (creature.brain.GetComponent<MLCombatBrain>() != null) return;
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"[MLCombatAgent] Model not found at: {modelPath}");
                return;
            }

            MLCombatBrain mlBrain = creature.brain.gameObject.AddComponent<MLCombatBrain>();
            mlBrain.LoadModelFromPath(modelPath);
            Debug.Log($"[MLCombatAgent] Brain attached to: {creature.name}");
        }
    }
}