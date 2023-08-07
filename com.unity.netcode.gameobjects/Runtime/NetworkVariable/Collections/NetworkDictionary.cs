﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Dictionaries
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary keys</typeparam>
    /// <typeparam name="TValue">The type for the dictionary values</typeparam>
    public class NetworkDictionary<TKey, TValue> : NetworkVariableBase
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public struct Enumerator : IEnumerator<(TKey Key, TValue Value)>
        {
            private NativeArray<TKey> keys;
            private NativeArray<TKey>.Enumerator keysEnumerator;
            private NativeArray<TValue> values;
            private NativeArray<TValue>.Enumerator valuesEnumerator;

            public (TKey Key, TValue Value) Current => (keysEnumerator.Current, valuesEnumerator.Current);

            object IEnumerator.Current => Current;

            public Enumerator(ref NativeList<TKey> keys, ref NativeList<TValue> values)
            {
                this.keys = keys.AsArray();
                this.values = values.AsArray();
                keysEnumerator = new NativeArray<TKey>.Enumerator(ref this.keys);
                valuesEnumerator = new NativeArray<TValue>.Enumerator(ref this.values);
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var keysEnumeratorCanMove = keysEnumerator.MoveNext();
                var valuesEnumeratorCanMove = valuesEnumerator.MoveNext();

                return keysEnumeratorCanMove && valuesEnumeratorCanMove;
            }

            public void Reset()
            {
                keysEnumerator.Reset();
                valuesEnumerator.Reset();
            }
        }

        private NativeList<TKey> m_Keys = new NativeList<TKey>(64, Allocator.Persistent);
        private NativeList<TValue> m_Values = new NativeList<TValue>(64, Allocator.Persistent);
        private NativeList<NetworkDictionaryEvent<TKey, TValue>> m_DirtyEvents = new NativeList<NetworkDictionaryEvent<TKey, TValue>>(64, Allocator.Persistent);

        /// <summary>
        /// Delegate type for dictionary changed event
        /// </summary>
        /// <param name="changeEvent">Struct containing information about the change event</param>
        public delegate void OnDictionaryChangedDelegate(NetworkDictionaryEvent<TKey, TValue> changeEvent);

        /// <summary>
        /// The callback to be invoked when the dictionary gets changed
        /// </summary>
        public event OnDictionaryChangedDelegate OnDictionaryChanged;

        /// <summary>
        /// Constructor method for <see cref="NetworkDictionary{TKey, TValue}" />
        /// </summary>
        public NetworkDictionary() { }

        /// <inheritdoc/>
        /// <param name="values"></param>
        /// <param name="readPerm"></param>
        /// <param name="writePerm"></param>
        public NetworkDictionary(
            IDictionary<TKey, TValue> values = default,
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm)
        {
            if (values != null)
            {
                foreach (var pair in values)
                {
                    m_Keys.Add(pair.Key);
                    m_Values.Add(pair.Value);
                }
            }
        }

        /// <inheritdoc />
        public override void ResetDirty()
        {
            base.ResetDirty();

            if (m_DirtyEvents.Length > 0)
            {
                m_DirtyEvents.Clear();
            }
        }

        /// <inheritdoc />
        public override bool IsDirty() => base.IsDirty() || m_DirtyEvents.Length > 0;

        internal void MarkNetworkObjectDirty()
        {
            if (m_NetworkBehaviour == null)
            {
                Debug.LogWarning("NetworkDictionary is written to, but doesn't know its NetworkBehaviour yet. " +
                                 "Are you modifying a NetworkDictionary before the NetworkObject is spawned?");
                return;
            }

            m_NetworkBehaviour.NetworkManager.MarkNetworkObjectDirty(m_NetworkBehaviour.NetworkObject);
        }

        /// <inheritdoc />
        public override void WriteDelta(FastBufferWriter writer)
        {
            if (base.IsDirty())
            {
                writer.WriteValueSafe((ushort)1);
                writer.WriteValueSafe(NetworkDictionaryEvent<TKey, TValue>.EventType.Full);
                WriteField(writer);

                return;
            }

            writer.WriteValueSafe((ushort)m_DirtyEvents.Length);

            for (int i = 0; i < m_DirtyEvents.Length; i++)
            {
                var element = m_DirtyEvents.ElementAt(i);
                writer.WriteValueSafe(m_DirtyEvents[i].Type);

                switch (m_DirtyEvents[i].Type)
                {
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            NetworkVariableSerialization<TKey>.Write(writer, ref element.Key);
                            NetworkVariableSerialization<TValue>.Write(writer, ref element.Value);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            NetworkVariableSerialization<TKey>.Write(writer, ref element.Key);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            NetworkVariableSerialization<TKey>.Write(writer, ref element.Key);
                            NetworkVariableSerialization<TValue>.Write(writer, ref element.Value);
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe((ushort)m_Keys.Length);

            for (int i = 0; i < m_Keys.Length; i++)
            {
                NetworkVariableSerialization<TKey>.Write(writer, ref m_Keys.ElementAt(i));
                NetworkVariableSerialization<TValue>.Write(writer, ref m_Values.ElementAt(i));
            }
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            m_Keys.Clear();
            m_Values.Clear();

            reader.ReadValueSafe(out ushort count);

            for (int i = 0; i < count; i++)
            {
                var key = new TKey();
                var value = new TValue();
                NetworkVariableSerialization<TKey>.Read(reader, ref key);
                NetworkVariableSerialization<TValue>.Read(reader, ref value);
                m_Keys.Add(key);
                m_Values.Add(value);
            }
        }

        /// <inheritdoc />
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            reader.ReadValueSafe(out ushort deltaCount);

            for (int i = 0; i < deltaCount; i++)
            {
                reader.ReadValueSafe(out NetworkDictionaryEvent<TKey, TValue>.EventType eventType);

                switch (eventType)
                {
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Add:
                        {
                            var key = new TKey();
                            var value = new TValue();
                            NetworkVariableSerialization<TKey>.Read(reader, ref key);
                            NetworkVariableSerialization<TValue>.Read(reader, ref value);

                            var index = m_Keys.IndexOf(key);

                            if (index == -1)
                            {
                                m_Keys.Add(key);
                                m_Values.Add(value);
                            }
                            else
                            {
                                m_Values[index] = value;
                            }

                            OnDictionaryChanged?.Invoke(new NetworkDictionaryEvent<TKey, TValue>
                            {
                                Type = eventType,
                                Key = key,
                                Value = value
                            });

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Remove:
                        {
                            var key = new TKey();
                            NetworkVariableSerialization<TKey>.Read(reader, ref key);
                            var index = m_Keys.IndexOf(key);

                            if (index == -1)
                            {
                                break;
                            }

                            var value = m_Values.ElementAt(index);
                            m_Keys.RemoveAt(index);
                            m_Values.RemoveAt(index);

                            OnDictionaryChanged?.Invoke(new NetworkDictionaryEvent<TKey, TValue>
                            {
                                Type = eventType,
                                Key = key,
                                Value = value
                            });

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Value:
                        {
                            var key = new TKey();
                            var value = new TValue();
                            NetworkVariableSerialization<TKey>.Read(reader, ref key);
                            NetworkVariableSerialization<TValue>.Read(reader, ref value);
                            var index = m_Keys.IndexOf(key);

                            if (index == -1)
                            {
                                throw new Exception("Shouldn't be here, key doesn't exist in dictionary");
                            }

                            var previousValue = m_Values.ElementAt(index);
                            m_Values[index] = value;

                            OnDictionaryChanged?.Invoke(new NetworkDictionaryEvent<TKey, TValue>
                            {
                                Type = eventType,
                                Key = key,
                                Value = value,
                                PreviousValue = previousValue
                            });

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>()
                                {
                                    Type = eventType,
                                    Key = key,
                                    Value = value,
                                    PreviousValue = previousValue
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Clear:
                        {
                            m_Keys.Clear();
                            m_Values.Clear();

                            OnDictionaryChanged?.Invoke(new NetworkDictionaryEvent<TKey, TValue>
                            {
                                Type = eventType
                            });

                            if (keepDirtyDelta)
                            {
                                m_DirtyEvents.Add(new NetworkDictionaryEvent<TKey, TValue>
                                {
                                    Type = eventType
                                });
                                MarkNetworkObjectDirty();
                            }
                        }
                        break;
                    case NetworkDictionaryEvent<TKey, TValue>.EventType.Full:
                        {
                            ReadField(reader);
                            ResetDirty();
                        }
                        break;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerator<(TKey Key, TValue Value)> GetEnumerator() => new Enumerator(ref m_Keys, ref m_Values);

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            if (!CanClientWrite(m_NetworkBehaviour.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkDictionary");
            }

            if (m_Keys.Contains(key))
            {
                throw new Exception("Shouldn't be here, key already exists in dictionary");
            }

            m_Keys.Add(key);
            m_Values.Add(value);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Add,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (!CanClientWrite(m_NetworkBehaviour.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkDictionary");
            }

            m_Keys.Clear();
            m_Values.Clear();

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Clear
            };

            HandleAddDictionaryEvent(dictionaryEvent);
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key) => m_Keys.Contains(key);

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            if (!CanClientWrite(m_NetworkBehaviour.NetworkManager.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkDictionary");
            }

            var index = m_Keys.IndexOf(key);

            if (index == -1)
            {
                return false;
            }

            var value = m_Values[index];
            m_Keys.RemoveAt(index);
            m_Values.RemoveAt(index);

            var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
            {
                Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Remove,
                Key = key,
                Value = value
            };

            HandleAddDictionaryEvent(dictionaryEvent);

            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = m_Keys.IndexOf(key);

            if (index == -1)
            {
                value = default;
                return false;
            }

            value = m_Values[index];
            return true;
        }

        /// <inheritdoc />
        public int Count => m_Keys.Length;

        /// <inheritdoc />
        public IEnumerable<TKey> Keys => m_Keys.ToArray();

        /// <inheritdoc />
        public IEnumerable<TValue> Values => m_Values.ToArray();

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                var index = m_Keys.IndexOf(key);

                if (index == -1)
                {
                    throw new Exception("Shouldn't be here, key doesn't exist in dictionary");
                }

                return m_Values[index];
            }
            set
            {
                if (!CanClientWrite(m_NetworkBehaviour.NetworkManager.LocalClientId))
                {
                    throw new InvalidOperationException("Client is not allowed to write to this NetworkDictionary");
                }

                var index = m_Keys.IndexOf(key);

                if (index == -1)
                {
                    Add(key, value);
                    return;
                }

                m_Values[index] = value;

                var dictionaryEvent = new NetworkDictionaryEvent<TKey, TValue>()
                {
                    Type = NetworkDictionaryEvent<TKey, TValue>.EventType.Value,
                    Key = key,
                    Value = value
                };

                HandleAddDictionaryEvent(dictionaryEvent);
            }
        }

        private void HandleAddDictionaryEvent(NetworkDictionaryEvent<TKey, TValue> dictionaryEvent)
        {
            m_DirtyEvents.Add(dictionaryEvent);
            MarkNetworkObjectDirty();
            OnDictionaryChanged?.Invoke(dictionaryEvent);
        }

        public override void Dispose()
        {
            m_Keys.Dispose();
            m_Values.Dispose();
            m_DirtyEvents.Dispose();
        }
    }

    /// <summary>
    /// Struct containing event information about changes to a NetworkDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type for the dictionary key that the event is about</typeparam>
    /// <typeparam name="TValue">The type for the dictionary value that the event is about</typeparam>
    public struct NetworkDictionaryEvent<TKey, TValue>
    {
        /// <summary>
        /// Enum representing the different operations available for triggering an event.
        /// </summary>
        public enum EventType : byte
        {
            /// <summary>
            /// Add
            /// </summary>
            Add = 0,

            /// <summary>
            /// Remove
            /// </summary>
            Remove = 1,

            /// <summary>
            /// Value changed
            /// </summary>
            Value = 2,

            /// <summary>
            /// Clear
            /// </summary>
            Clear = 3,

            /// <summary>
            /// Full dictionary refresh
            /// </summary>
            Full = 4
        }

        /// <summary>
        /// Enum representing the operation made to the dictionary.
        /// </summary>
        public EventType Type;

        /// <summary>
        /// The key changed, added or removed if available.
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The value changed, added or removed if available.
        /// </summary>
        public TValue Value;

        /// <summary>
        /// The previous value when "Value" has changed, if available.
        /// </summary>
        public TValue PreviousValue;
    }
}