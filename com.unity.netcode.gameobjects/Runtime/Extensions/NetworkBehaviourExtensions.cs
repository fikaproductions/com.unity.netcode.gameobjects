using System;
using System.Collections;
using UnityEngine;

namespace Unity.Netcode
{
    public static class NetworkBehaviourExtensions
    {
        public static Coroutine ExecuteWhenSpawned(this NetworkBehaviour self, Action action)
        {
            if (self.IsSpawned)
            {
                action?.Invoke();
                return null;
            }

            return self.StartCoroutine(ExecuteWhenSpawned());

            IEnumerator ExecuteWhenSpawned()
            {
                yield return new WaitUntil(() => self.IsSpawned);

                action?.Invoke();
            }
        }
    }
}
