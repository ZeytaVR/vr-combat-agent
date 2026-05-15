# VR Combat Agent — Unity ML-Agents

A PPO-trained combat agent built in Unity ML-Agents, designed as the foundation for an adaptive NPC AI system in Blade & Sorcery.

## What This Is

This project trains a reinforcement learning agent to fight a moving enemy in a controlled Unity arena. The trained policy is exported as ONNX and will eventually be loaded into a Blade & Sorcery mod where NPCs learn from the player's fighting patterns between sessions and adapt to counter them.

## Environment

- **Arena:** 50x50 flat plane
- **Agent:** capsule with Rigidbody, controlled by PPO policy
- **Enemy:** capsule with random movement (changes direction every 2 seconds)
- **Combat:** agent has 100HP, enemy has 100HP, 25 damage per hit, 4 hits to kill
- **Observations (12):** agent position, agent velocity, enemy position, distance to enemy, agent health, enemy health
- **Actions:** continuous movement (X/Z), discrete attack

## Training Results

Three progressive training runs, each increasing environment complexity:

| Run | Environment | Converged Reward | Steps to Converge |
|-----|-------------|-----------------|-------------------|
| combat-test-07 | Stationary enemy | 0.992 | ~80k              |
| combat-test-08 | Moving enemy | 0.991 | ~300k             |
| combat-test-09 | Moving enemy + health/damage | 2.112 | ~120k             |
| combat-test-10 | Moving enemy + health/damage + distance shaping | 3.239 | ~150k (Std 0.33) |
| combat-test-11 | Moving enemy + health/damage + distance shaping + dodge/strafe penalty | 3.245 | ~200k (Std 0.41) |

The reward increase in run 3 reflects the richer reward structure — +0.3 per hit, +1.0 for kill, -1.0 for death. Run 4 adds distance shaping, producing higher rewards with intentional behavioral variety (Std ~0.33) rather than a single deterministic strategy. Run 5 adds a dodge/strafe penalty, forcing dynamic approach-retreat behavior rather than camping at attack range.

## Stack

- Unity 6.4 (6000.4.6f1)
- ML-Agents 4.0.3 (Unity package) + 1.1.0 (Python)
- PyTorch 2.7.0+cu128
- Python 3.10.12

## Unity Project

The Unity scene and C# scripts: [vr-combat-agent-unity](https://github.com/ZeytaVR/vr-combat-agent-unity)

## Roadmap

- [ ] Load trained ONNX policy into Blade & Sorcery mod
- [ ] Log player behavioral data during fights
- [ ] Between-session fine-tuning pipeline
- [ ] Adaptive NPC that counters player-specific patterns