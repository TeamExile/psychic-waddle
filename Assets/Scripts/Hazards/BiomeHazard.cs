using UnityEngine;
using Unity.Netcode;

namespace Friendslop.Hazards
{
    /// <summary>
    /// Base class for environmental hazards in biomes.
    /// Provides common functionality for network synchronization and player detection.
    /// </summary>
    public abstract class BiomeHazard : NetworkBehaviour
    {
        [Header("Hazard Settings")]
        [SerializeField] protected int damage = 10;
        [SerializeField] protected float damageInterval = 1f;
        [SerializeField] protected bool isActive = true;

        [Header("Debug Visualization")]
        [SerializeField] protected Color hazardColor = Color.red;
        [SerializeField] protected Color activeColor = Color.yellow;
        [SerializeField] protected Color inactiveColor = Color.gray;

        protected NetworkVariable<bool> _networkIsActive = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        protected Renderer _renderer;
        protected Color _originalColor;

        public bool IsHazardActive => _networkIsActive.Value;

        protected virtual void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _networkIsActive.OnValueChanged += OnActiveStateChanged;

            if (IsServer)
            {
                _networkIsActive.Value = isActive;
            }

            UpdateVisuals();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _networkIsActive.OnValueChanged -= OnActiveStateChanged;
        }

        protected virtual void OnActiveStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisuals();
        }

        /// <summary>
        /// Updates visual appearance based on hazard state.
        /// Override for custom visual effects.
        /// </summary>
        protected virtual void UpdateVisuals()
        {
            if (_renderer != null)
            {
                _renderer.material.color = _networkIsActive.Value ? activeColor : inactiveColor;
            }
        }

        /// <summary>
        /// Apply damage to a player. Server-only.
        /// </summary>
        protected virtual void ApplyDamageToPlayer(PlayerHealth playerHealth)
        {
            if (!IsServer) return;
            if (playerHealth == null) return;
            if (!_networkIsActive.Value) return;

            playerHealth.TakeDamage(damage, GetHazardName());
            OnDamageApplied(playerHealth);
        }

        /// <summary>
        /// Called after damage is applied. Override for custom effects.
        /// </summary>
        protected virtual void OnDamageApplied(PlayerHealth playerHealth)
        {
            // Notify all clients of the damage effect
            PlayDamageEffectClientRpc(playerHealth.transform.position);
        }

        [ClientRpc]
        protected virtual void PlayDamageEffectClientRpc(Vector3 position)
        {
            // Override in derived classes for custom effects
            Debug.Log($"Hazard {GetHazardName()} dealt damage at {position}");
        }

        /// <summary>
        /// Activate or deactivate the hazard. Server-only.
        /// </summary>
        public void SetActive(bool active)
        {
            if (!IsServer) return;
            _networkIsActive.Value = active;
        }

        /// <summary>
        /// Returns the hazard type name for logging and identification.
        /// </summary>
        protected abstract string GetHazardName();

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = isActive ? hazardColor : inactiveColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
