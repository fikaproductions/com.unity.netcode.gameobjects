using UnityEngine;
using System.Reflection;

namespace Unity.Netcode.Components
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CanCommitToTransform = IsOwner;

            // [PATCH] https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1560
            if (CanCommitToTransform)
            {
                m_HasSentLastValue = true;
            }
        }

        protected override void Update()
        {
            CanCommitToTransform = IsOwner;
            base.Update();

            if (NetworkManager.Singleton != null &&
                (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening) &&
                CanCommitToTransform)
            {
                TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
            }
        }
    }
}
