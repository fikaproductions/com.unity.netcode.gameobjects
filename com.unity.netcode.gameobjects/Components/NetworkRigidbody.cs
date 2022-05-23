#if COM_UNITY_MODULES_PHYSICS
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// <para>NetworkRigidbody allows for the use of <see cref="Rigidbody"/> on network objects. By controlling the kinematic
    /// mode of the rigidbody and disabling it on all peers but the authoritative one.</para>
    /// <para>Has the logic of the NetworkRigidbody of the 1.0.0-pre.7 version.</para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class NetworkRigidbody : NetworkBehaviour
    {
        private Rigidbody m_Rigidbody;
        private NetworkTransform m_NetworkTransform;

        private bool m_OriginalKinematic;
        private RigidbodyInterpolation m_OriginalInterpolation;

        private bool m_Awoken; // [PATCH] https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1757
        private bool m_IsAuthority; // Used to cache the authority state of this rigidbody during the last frame

        /// <summary>
        /// Gets a bool value indicating whether this <see cref="NetworkRigidbody"/> on this peer currently holds authority.
        /// </summary>
        private bool HasAuthority => m_NetworkTransform.CanCommitToTransform;

        private void Awake()
        {
            // [PATCH] https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1757
            if (!m_Awoken)
            {
                m_Rigidbody = GetComponent<Rigidbody>();
                m_NetworkTransform = GetComponent<NetworkTransform>();
                m_Awoken = true;
            }
        }

        // [PATCH] Restore networked behaviour if it was disabled for local physics.
        private void OnEnable()
        {
            if (NetworkManager.IsListening && IsSpawned)
            {
                UpdateRigidbodyKinematicMode();
            }
        }

        // [PATCH] Allow local physics when disabled.
        private void OnDisable()
        {
            m_Rigidbody.isKinematic = m_OriginalKinematic;
            m_Rigidbody.interpolation = m_OriginalInterpolation;
        }

        private void FixedUpdate()
        {
            if (NetworkManager.IsListening)
            {
                if (HasAuthority != m_IsAuthority)
                {
                    m_IsAuthority = HasAuthority;
                    UpdateRigidbodyKinematicMode();
                }
            }
        }

        // Puts the rigidbody in a kinematic non-interpolated mode on everyone but the server.
        private void UpdateRigidbodyKinematicMode()
        {
            if (m_IsAuthority == false)
            {
                m_OriginalKinematic = m_Rigidbody.isKinematic;
                m_Rigidbody.isKinematic = true;

                m_OriginalInterpolation = m_Rigidbody.interpolation;
                // Set interpolation to none, the NetworkTransform component interpolates the position of the object.
                m_Rigidbody.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                // Resets the rigidbody back to it's non replication only state. Happens on shutdown and when authority is lost
                m_Rigidbody.isKinematic = m_OriginalKinematic;
                m_Rigidbody.interpolation = m_OriginalInterpolation;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            // [PATCH] https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1757
            Awake();

            m_IsAuthority = HasAuthority;
            m_OriginalKinematic = m_Rigidbody.isKinematic;
            m_OriginalInterpolation = m_Rigidbody.interpolation;
            UpdateRigidbodyKinematicMode();
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            // [PATCH] https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1754
            if (this && GetComponent<Rigidbody>())
            {
                UpdateRigidbodyKinematicMode();
            }
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
