using Unity.Netcode.Components;
using UnityEditor;

namespace Unity.Netcode.Editor
{
    [CustomEditor(typeof(ClientNetworkTransform), true)]
    [CanEditMultipleObjects]
    public class ClientNetworkTransformEditor : NetworkTransformEditor
    {
    }
}
