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

        // Rôle actif lu depuis le Singleton RoleManager
        public RoleData ActiveRole => RoleManager.Instance?.CurrentRole;

        // Accesseurs lecture seule
        public float CurrentHealth => _currentHealth;
        public float MaxHealth     => maxHealth;

        // Événements : (currentHealth, maxHealth) et mort
        public event Action<float, float> OnHealthChanged;
        public event Action               OnDeath;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        // Inflige des dégâts au joueur
        public void TakeDamage(float amount)
        {
            if (_currentHealth <= 0f || amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0f)
            {
                // Vérifie si la potion Vie peut ressusciter le joueur
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

        // Soigne le joueur sans dépasser le maximum
        public void Heal(float amount)
        {
            if (_currentHealth <= 0f || amount <= 0f) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        // Remet le joueur à pleine santé
        public void ResetState()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
    }
}
