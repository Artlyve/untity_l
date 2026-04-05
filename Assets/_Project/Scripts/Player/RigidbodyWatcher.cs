using UnityEngine;

namespace ProjectFPS.Player
{
    /// <summary>
    /// Diagnostique : surveille un Rigidbody et logue toute modification de ses valeurs clés.
    ///
    /// Ajoutez ce composant sur le GameObject qui possède un Rigidbody.
    /// Consultez la Console pour identifier quel script modifie les valeurs.
    ///
    /// Supprimez ce composant une fois le coupable identifié.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyWatcher : MonoBehaviour
    {
        private Rigidbody _rb;

        // Valeurs snapshot de la frame précédente
        private float _prevMass;
        private float _prevDrag;
        private float _prevAngularDrag;
        private bool  _prevIsKinematic;
        private bool  _prevUseGravity;
        private RigidbodyConstraints _prevConstraints;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            TakeSnapshot();

            Debug.Log($"[RigidbodyWatcher] Surveillance démarrée sur '{gameObject.name}' :" +
                $"\n  mass={_rb.mass}  drag={_rb.linearDamping}  angularDrag={_rb.angularDamping}" +
                $"\n  isKinematic={_rb.isKinematic}  useGravity={_rb.useGravity}" +
                $"\n  constraints={_rb.constraints}");
        }

        private void LateUpdate()
        {
            bool changed = false;

            if (!Mathf.Approximately(_rb.mass, _prevMass))
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' mass changé : {_prevMass} → {_rb.mass}", this);
                changed = true;
            }
            if (!Mathf.Approximately(_rb.linearDamping, _prevDrag))
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' drag changé : {_prevDrag} → {_rb.linearDamping}", this);
                changed = true;
            }
            if (!Mathf.Approximately(_rb.angularDamping, _prevAngularDrag))
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' angularDrag changé : {_prevAngularDrag} → {_rb.angularDamping}", this);
                changed = true;
            }
            if (_rb.isKinematic != _prevIsKinematic)
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' isKinematic changé : {_prevIsKinematic} → {_rb.isKinematic}", this);
                changed = true;
            }
            if (_rb.useGravity != _prevUseGravity)
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' useGravity changé : {_prevUseGravity} → {_rb.useGravity}", this);
                changed = true;
            }
            if (_rb.constraints != _prevConstraints)
            {
                Debug.LogWarning($"[RigidbodyWatcher] '{gameObject.name}' constraints changé : {_prevConstraints} → {_rb.constraints}", this);
                changed = true;
            }

            if (changed)
            {
                // Affiche la callstack complète pour identifier le script responsable
                Debug.LogWarning($"[RigidbodyWatcher] StackTrace de la frame :\n{System.Environment.StackTrace}", this);
                TakeSnapshot();
            }
        }

        private void TakeSnapshot()
        {
            _prevMass        = _rb.mass;
            _prevDrag        = _rb.linearDamping;
            _prevAngularDrag = _rb.angularDamping;
            _prevIsKinematic = _rb.isKinematic;
            _prevUseGravity  = _rb.useGravity;
            _prevConstraints = _rb.constraints;
        }
    }
}
