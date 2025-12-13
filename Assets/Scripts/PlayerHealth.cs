using UnityEngine;
using Unity.Netcode;
using System;

namespace Friendslop
{
    /// <summary>
    /// Networked player health system.
    /// Handles damage, death, and respawning for multiplayer gameplay.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invulnerabilityDuration = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private float damageFlashDuration = 0.2f;

        private NetworkVariable<int> _currentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float _lastDamageTime;
        private Renderer _renderer;
        private Material _material;
        private Color _originalColor;
        private float _flashTimer;

        public int CurrentHealth => _currentHealth.Value;
        public int MaxHealth => maxHealth;
        public bool IsDead => _currentHealth.Value <= 0;

        public event Action<int, int> OnHealthChanged; // current, max
        public event Action OnDeath;
        public event Action OnDamaged;

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                // Cache the material to avoid creating new instances
                _material = _renderer.material;
                _originalColor = _material.color;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentHealth.OnValueChanged += HandleHealthChanged;

            if (IsServer)
            {
                _currentHealth.Value = maxHealth;
            }

            OnHealthChanged?.Invoke(_currentHealth.Value, maxHealth);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _currentHealth.OnValueChanged -= HandleHealthChanged;
        }

        private void Update()
        {
            // Handle damage flash visual effect
            if (_flashTimer > 0)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0 && _material != null)
                {
                    _material.color = _originalColor;
                }
            }
        }

        /// <summary>
        /// Apply damage to this player. Only call on server.
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="sourceId">Optional source identifier for damage (hazard type, player ID, etc.)</param>
        public void TakeDamage(int damage, string sourceId = "")
        {
            if (!IsServer) return;
            if (IsDead) return;

            // Check invulnerability
            if (Time.time - _lastDamageTime < invulnerabilityDuration)
            {
                return;
            }

            _lastDamageTime = Time.time;
            _currentHealth.Value = Mathf.Max(0, _currentHealth.Value - damage);

            Debug.Log($"Player took {damage} damage from {sourceId}. Health: {_currentHealth.Value}/{maxHealth}");

            // Notify all clients about damage effect
            TakeDamageClientRpc();

            if (_currentHealth.Value <= 0)
            {
                HandleDeath();
            }
        }

        /// <summary>
        /// Heal this player. Only call on server.
        /// </summary>
        /// <param name="amount">Amount of health to restore</param>
        public void Heal(int amount)
        {
            if (!IsServer) return;
            if (IsDead) return;

            _currentHealth.Value = Mathf.Min(maxHealth, _currentHealth.Value + amount);
        }

        /// <summary>
        /// Reset health to full. Only call on server.
        /// </summary>
        public void ResetHealth()
        {
            if (!IsServer) return;
            _currentHealth.Value = maxHealth;
        }

        private void HandleHealthChanged(int previousValue, int newValue)
        {
            OnHealthChanged?.Invoke(newValue, maxHealth);

            if (newValue < previousValue)
            {
                OnDamaged?.Invoke();
            }
        }

        private void HandleDeath()
        {
            Debug.Log($"Player {OwnerClientId} died!");
            OnDeathClientRpc();

            // For now, just respawn with full health after a short delay
            // This can be expanded later with proper respawn logic
            Invoke(nameof(RespawnPlayer), 3f);
        }

        private void RespawnPlayer()
        {
            if (!IsServer) return;
            _currentHealth.Value = maxHealth;
            Debug.Log($"Player {OwnerClientId} respawned!");
        }

        [ClientRpc]
        private void TakeDamageClientRpc()
        {
            // Flash damage color
            if (_material != null)
            {
                _material.color = damageFlashColor;
                _flashTimer = damageFlashDuration;
            }

            OnDamaged?.Invoke();
        }

        [ClientRpc]
        private void OnDeathClientRpc()
        {
            OnDeath?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw health bar in editor
            Gizmos.color = Color.Lerp(Color.red, Color.green, (float)CurrentHealth / maxHealth);
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireCube(healthBarPos, new Vector3(1f, 0.2f, 0.1f));
        }
    }
}
