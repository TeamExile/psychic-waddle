using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Friendslop.Hazards
{
    /// <summary>
    /// Toxic mist zone that damages players while they remain inside.
    /// The mist periodically pulses and applies damage over time.
    /// </summary>
    public class ToxicMistZone : BiomeHazard
    {
        [Header("Toxic Mist Settings")]
        [SerializeField] private float zoneRadius = 3f;
        [SerializeField] private float zoneHeight = 2f;
        [SerializeField] private float pulseDuration = 2f;

        [Header("Visual Settings")]
        [SerializeField] private Color mistColorMin = new Color(0.2f, 0.8f, 0.2f, 0.2f);
        [SerializeField] private Color mistColorMax = new Color(0.4f, 1f, 0.3f, 0.5f);
        [SerializeField] private float pulseSpeed = 1f;

        [Header("Detection")]
        [SerializeField] private LayerMask playerLayerMask = ~0;

        private Dictionary<PlayerHealth, float> _playersInZone = new Dictionary<PlayerHealth, float>();
        private float _pulseTimer;
        private bool _isPulsing;

        protected override void Awake()
        {
            base.Awake();

            // Ensure we have a trigger collider
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
            }
            sphereCollider.isTrigger = true;
            sphereCollider.radius = zoneRadius;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _pulseTimer = pulseDuration;
            }

            // Set initial visual scale
            UpdateZoneScale();
        }

        private void Update()
        {
            if (IsServer && _networkIsActive.Value)
            {
                UpdateDamageToPlayersInZone();
                UpdatePulse();
            }

            // Visual animation (runs on all clients)
            AnimateMist();
        }

        private void UpdatePulse()
        {
            _pulseTimer -= Time.deltaTime;
            if (_pulseTimer <= 0)
            {
                _pulseTimer = pulseDuration;
                _isPulsing = true;
                PulseEffectClientRpc();
            }
        }

        private void UpdateDamageToPlayersInZone()
        {
            // Create a list of players to remove (those who left the zone)
            List<PlayerHealth> playersToRemove = new List<PlayerHealth>();

            foreach (var kvp in _playersInZone)
            {
                PlayerHealth playerHealth = kvp.Key;

                if (playerHealth == null || !IsPlayerInZone(playerHealth.transform.position))
                {
                    playersToRemove.Add(playerHealth);
                    continue;
                }

                // Update damage timer
                float timeSinceLastDamage = Time.time - _playersInZone[playerHealth];
                if (timeSinceLastDamage >= damageInterval)
                {
                    ApplyDamageToPlayer(playerHealth);
                    _playersInZone[playerHealth] = Time.time;
                }
            }

            // Remove players who left the zone
            foreach (var player in playersToRemove)
            {
                _playersInZone.Remove(player);
            }
        }

        private bool IsPlayerInZone(Vector3 playerPosition)
        {
            Vector3 zoneCenter = transform.position;
            float horizontalDistance = Vector2.Distance(
                new Vector2(playerPosition.x, playerPosition.z),
                new Vector2(zoneCenter.x, zoneCenter.z)
            );
            float verticalDistance = Mathf.Abs(playerPosition.y - zoneCenter.y);

            return horizontalDistance <= zoneRadius && verticalDistance <= zoneHeight;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if (!_networkIsActive.Value) return;

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null && !_playersInZone.ContainsKey(playerHealth))
            {
                _playersInZone[playerHealth] = Time.time - damageInterval; // Allow immediate first damage
                PlayerEnteredZoneClientRpc(playerHealth.OwnerClientId);
                Debug.Log($"Player entered toxic mist zone at {transform.position}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = other.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null && _playersInZone.ContainsKey(playerHealth))
            {
                _playersInZone.Remove(playerHealth);
                PlayerExitedZoneClientRpc(playerHealth.OwnerClientId);
                Debug.Log($"Player exited toxic mist zone at {transform.position}");
            }
        }

        private void AnimateMist()
        {
            if (_renderer == null) return;

            // Pulsing color effect
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            Color currentColor = Color.Lerp(mistColorMin, mistColorMax, pulse);

            if (!_networkIsActive.Value)
            {
                currentColor = inactiveColor;
            }

            _renderer.material.color = currentColor;

            // Subtle scale pulsing
            float scalePulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 0.5f) * 0.05f;
            transform.localScale = new Vector3(
                zoneRadius * 2f * scalePulse,
                zoneHeight * scalePulse,
                zoneRadius * 2f * scalePulse
            );
        }

        private void UpdateZoneScale()
        {
            transform.localScale = new Vector3(zoneRadius * 2f, zoneHeight, zoneRadius * 2f);
        }

        [ClientRpc]
        private void PulseEffectClientRpc()
        {
            _isPulsing = true;
            // Pulse effect handled in AnimateMist
        }

        [ClientRpc]
        private void PlayerEnteredZoneClientRpc(ulong playerId)
        {
            Debug.Log($"Player {playerId} entered toxic mist!");
        }

        [ClientRpc]
        private void PlayerExitedZoneClientRpc(ulong playerId)
        {
            Debug.Log($"Player {playerId} escaped toxic mist!");
        }

        protected override string GetHazardName()
        {
            return "ToxicMist";
        }

        protected override void OnDrawGizmos()
        {
            // Draw zone bounds
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawSphere(transform.position, zoneRadius);

            // Draw cylinder outline
            Gizmos.color = mistColorMin;
            Vector3 size = new Vector3(zoneRadius * 2f, zoneHeight, zoneRadius * 2f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
