using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    public class DerefencingListUpdator
    {
        private readonly MonoBehaviour asyncUtils;
        private readonly Dictionary<object, Coroutine> pendingUpdates = new Dictionary<object, Coroutine>();
        private readonly NetworkList<NetworkBehaviourReference> references;

        public DerefencingListUpdator(MonoBehaviour asyncUtils, NetworkList<NetworkBehaviourReference> references)
        {
            this.asyncUtils = asyncUtils;
            this.references = references;
        }

        public void UpdateList<T>(IList<T> target) where T : NetworkBehaviour
        {
            if (pendingUpdates.TryGetValue(target, out var pendingUpdate))
            {
                if (pendingUpdate != null)
                {
                    asyncUtils.StopCoroutine(pendingUpdate);
                }

                pendingUpdates.Remove(target);
            }

            pendingUpdates.Add(target, asyncUtils.StartCoroutine(UpdateList()));

            IEnumerator UpdateList()
            {
                var index = 0;

                foreach (var reference in references.ToArray())
                {
                    if (!reference.TryGet(out T item))
                    {
                        yield return new WaitUntil(() => reference.TryGet(out item));
                    }

                    if (index < target.Count)
                    {
                        target[index] = item;
                    }
                    else
                    {
                        target.Add(item);
                    }

                    index++;
                }

                if (index < target.Count)
                {
                    target.RemoveAt(index);
                }

                pendingUpdates.Remove(target);
            }
        }

        public NetworkList<NetworkBehaviourReference>.OnListChangedDelegate GetListUpdator<T>(IList<T> target) where T : NetworkBehaviour =>
            (NetworkListEvent<NetworkBehaviourReference> _) => UpdateList(target);
    }
}
