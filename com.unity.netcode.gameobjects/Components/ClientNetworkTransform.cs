using UnityEngine;

namespace Unity.Netcode.Components
{
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
