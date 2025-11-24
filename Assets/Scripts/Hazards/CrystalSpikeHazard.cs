using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Friendslop.Hazards
{
    /// <summary>
    /// Crystal spikes that periodically erupt from the ground and damage players.
    /// The spikes cycle between dormant and active states, synced across all clients.
    /// </summary>
    public class CrystalSpikeHazard : BiomeHazard
    {
        [Header("Crystal Spike Settings")]
        [SerializeField] private float eruptionInterval = 3f;
        [SerializeField] private float warningDuration = 1f;
        [SerializeField] private float activeDuration = 1.5f;
        [SerializeField] private float spikeHeight = 2f;

        [Header("Visual Settings")]
        [SerializeField] private Color dormantColor = new Color(0.5f, 0.2f, 0.8f, 0.5f);
        [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color eruptedColor = new Color(0.8f, 0.1f, 0.3f, 1f);

        [Header("Detection")]
        [SerializeField] private float damageRadius = 1.5f;
        [SerializeField] private LayerMask playerLayerMask = ~0;

        private NetworkVariable<SpikeState> _spikeState = new NetworkVariable<SpikeState>(
            SpikeState.Dormant,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float _stateTimer;
        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private HashSet<PlayerHealth> _damagedPlayers = new HashSet<PlayerHealth>();

        public enum SpikeState
        {
            Dormant,
            Warning,
            Erupting,
            Active
        }

        public SpikeState CurrentState => _spikeState.Value;

        protected override void Awake()
        {
            base.Awake();
            _originalScale = transform.localScale;
            _originalPosition = transform.position;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _spikeState.OnValueChanged += OnSpikeStateChanged;

            if (IsServer)
            {
                _spikeState.Value = SpikeState.Dormant;
                _stateTimer = eruptionInterval;
            }

            UpdateSpikeVisuals();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _spikeState.OnValueChanged -= OnSpikeStateChanged;
        }

        private void Update()
        {
            if (IsServer && _networkIsActive.Value)
            {
                UpdateSpikeStateServer();
            }

            // Animate spike based on state (client-side interpolation)
            AnimateSpike();
        }

        private void FixedUpdate()
        {
            if (IsServer && _spikeState.Value == SpikeState.Active)
            {
                CheckForPlayersInRange();
            }
        }

        private void UpdateSpikeStateServer()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer <= 0)
            {
                switch (_spikeState.Value)
                {
                    case SpikeState.Dormant:
                        _spikeState.Value = SpikeState.Warning;
                        _stateTimer = warningDuration;
                        break;

                    case SpikeState.Warning:
                        _spikeState.Value = SpikeState.Erupting;
                        _stateTimer = 0.2f; // Quick eruption animation
                        break;

                    case SpikeState.Erupting:
                        _spikeState.Value = SpikeState.Active;
                        _stateTimer = activeDuration;
                        _damagedPlayers.Clear();
                        break;

                    case SpikeState.Active:
                        _spikeState.Value = SpikeState.Dormant;
                        _stateTimer = eruptionInterval;
                        break;
                }
            }
        }

        private void OnSpikeStateChanged(SpikeState previousState, SpikeState newState)
        {
            UpdateSpikeVisuals();

            if (newState == SpikeState.Erupting)
            {
                PlayEruptionEffectClientRpc();
            }
        }

        private void AnimateSpike()
        {
            float targetHeight;
            float animSpeed = 10f;

            switch (_spikeState.Value)
            {
                case SpikeState.Dormant:
                    targetHeight = 0.1f;
                    break;
                case SpikeState.Warning:
                    // Pulsing effect during warning
                    float pulse = Mathf.Sin(Time.time * 10f) * 0.1f + 0.2f;
                    targetHeight = pulse;
                    break;
                case SpikeState.Erupting:
                case SpikeState.Active:
                    targetHeight = 1f;
                    animSpeed = 20f;
                    break;
                default:
                    targetHeight = 0.1f;
                    break;
            }

            // Animate scale
            Vector3 targetScale = new Vector3(
                _originalScale.x,
                _originalScale.y * (targetHeight * spikeHeight),
                _originalScale.z
            );
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

            // Adjust position to keep base at original position
            float heightDiff = (transform.localScale.y - _originalScale.y * 0.1f) / 2f;
            transform.position = new Vector3(
                _originalPosition.x,
                _originalPosition.y + heightDiff,
                _originalPosition.z
            );
        }

        private void UpdateSpikeVisuals()
        {
            if (_renderer == null) return;

            switch (_spikeState.Value)
            {
                case SpikeState.Dormant:
                    _renderer.material.color = dormantColor;
                    break;
                case SpikeState.Warning:
                    _renderer.material.color = warningColor;
                    break;
                case SpikeState.Erupting:
                case SpikeState.Active:
                    _renderer.material.color = eruptedColor;
                    break;
            }
        }

        private void CheckForPlayersInRange()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius, playerLayerMask);

            foreach (var collider in colliders)
            {
                PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = collider.GetComponentInParent<PlayerHealth>();
                }

                if (playerHealth != null && !_damagedPlayers.Contains(playerHealth))
                {
                    ApplyDamageToPlayer(playerHealth);
                    _damagedPlayers.Add(playerHealth);
                }
            }
        }

        [ClientRpc]
        private void PlayEruptionEffectClientRpc()
        {
            // Simple eruption effect - can be replaced with particles later
            Debug.Log($"Crystal Spike erupted at {transform.position}!");
        }

        protected override string GetHazardName()
        {
            return "CrystalSpike";
        }

        protected override void OnDrawGizmos()
        {
            // Draw damage radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, damageRadius);

            // Draw spike shape
            Gizmos.color = dormantColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, spikeHeight, 0.5f));
        }
    }
}
