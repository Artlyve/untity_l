using System.Collections.Generic;
using UnityEngine;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Diagnostique : affiche tous les paramètres de l'Animator dans la Console
    /// à chaque fois qu'une valeur change.
    ///
    /// Utilisation :
    ///   1. Ajoutez ce composant sur le même GameObject que l'Animator.
    ///   2. Lancez le jeu — la Console affiche les changements en temps réel.
    ///   3. Appuyez sur [F9] pour activer/désactiver le monitoring en Play Mode.
    ///   4. Supprimez ce composant quand vous n'en avez plus besoin.
    ///
    /// Il ne fait QUE lire — aucun impact sur le gameplay.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorDebugger : MonoBehaviour
    {
        [SerializeField] private bool  enableOnStart = true;
        [SerializeField] private KeyCode toggleKey   = KeyCode.F9;
        [Tooltip("N'affiche les logs que si la valeur a changé de plus de ce seuil (pour les floats).")]
        [SerializeField] private float floatChangeThreshold = 0.01f;

        private Animator _animator;
        private bool     _enabled;

        // Snapshots des valeurs précédentes
        private readonly Dictionary<string, float> _prevFloat  = new();
        private readonly Dictionary<string, bool>  _prevBool   = new();
        private readonly Dictionary<string, int>   _prevInt    = new();

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _enabled  = enableOnStart;
        }

        private void Start()
        {
            Debug.Log($"[AnimatorDebugger] Attaché à '{gameObject.name}' | {_animator.parameterCount} paramètres détectés." +
                $"\n  Appuyez sur [{toggleKey}] pour activer/désactiver.");
            TakeSnapshot();
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _enabled = !_enabled;
                Debug.Log($"[AnimatorDebugger] {(_enabled ? "ON" : "OFF")}");
            }

            if (!_enabled || _animator == null) return;

            for (int i = 0; i < _animator.parameterCount; i++)
            {
                AnimatorControllerParameter p = _animator.parameters[i];

                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float:
                    {
                        float val = _animator.GetFloat(p.name);
                        if (!_prevFloat.TryGetValue(p.name, out float prev)
                            || Mathf.Abs(val - prev) > floatChangeThreshold)
                        {
                            Debug.Log($"[Anim] {p.name} (float) : {prev:F3} → {val:F3}");
                            _prevFloat[p.name] = val;
                        }
                        break;
                    }
                    case AnimatorControllerParameterType.Bool:
                    {
                        bool val = _animator.GetBool(p.name);
                        if (!_prevBool.TryGetValue(p.name, out bool prev) || val != prev)
                        {
                            Debug.Log($"[Anim] {p.name} (bool) : {prev} → {val}");
                            _prevBool[p.name] = val;
                        }
                        break;
                    }
                    case AnimatorControllerParameterType.Int:
                    {
                        int val = _animator.GetInteger(p.name);
                        if (!_prevInt.TryGetValue(p.name, out int prev) || val != prev)
                        {
                            Debug.Log($"[Anim] {p.name} (int) : {prev} → {val}");
                            _prevInt[p.name] = val;
                        }
                        break;
                    }
                    // Triggers : pas trackés (ils se réinitialisent seuls)
                }
            }
        }

        private void TakeSnapshot()
        {
            if (_animator == null) return;

            for (int i = 0; i < _animator.parameterCount; i++)
            {
                AnimatorControllerParameter p = _animator.parameters[i];
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Float:
                        _prevFloat[p.name] = _animator.GetFloat(p.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        _prevBool[p.name] = _animator.GetBool(p.name);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _prevInt[p.name] = _animator.GetInteger(p.name);
                        break;
                }
            }
        }
    }
}
