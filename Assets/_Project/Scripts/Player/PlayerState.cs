using System;
using UnityEngine;
using ProjectFPS.Roles;
using ProjectFPS.Inventory;

namespace ProjectFPS.Player
{
    public class PlayerState : MonoBehaviour
    {
        [Header("Santé")]
        [SerializeField] private float maxHealth = 100f;

        private float _currentHealth;

        public RoleData ActiveRole => RoleManager.Instance?.CurrentRole;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth     => maxHealth;

        // Événements
        public event Action<float, float> OnHealthChanged;
        public event Action               OnDamageReceived; // ← nouveau : déclenche l'animation de coup
        public event Action               OnDeath;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (_currentHealth <= 0f || amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Déclenche l'animation de coup reçu
            OnDamageReceived?.Invoke();

            if (_currentHealth <= 0f)
            {
                var effects = GetComponent<EffectSystem>();
                if (effects != null && effects.TryRevive())
                {
                    Debug.Log($"[PlayerState] {name} serait mort, mais la potion Vie l'a sauvé !");
                    return;
                }

                Debug.Log($"[PlayerState] {name} est mort (PV = 0).");
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (_currentHealth <= 0f || amount <= 0f) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void ResetState()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }
}
