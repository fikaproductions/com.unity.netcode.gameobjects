namespace Unity.Netcode
{
    public class NetworkSerializableVariable<T> : NetworkVariableBase where T : INetworkSerializable, new()
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(T newValue);

        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Constructor for <see cref="NetworkSerializableVariable{T}"/>
        /// </summary>
        /// <param name="value">initial value set that is of type T</param>
        /// <param name="readPerm">the <see cref="NetworkVariableReadPermission"/> for this <see cref="NetworkSerializableVariable{T}"/></param>
        /// <param name="writePerm">the <see cref="NetworkVariableWritePermission"/> for this <see cref="NetworkSerializableVariable{T}"/></param>
        public NetworkSerializableVariable(T value = default,
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm) => m_InternalValue = value ?? new T();

        /// <summary>
        /// The internal value of the NetworkSerializableVariable
        /// </summary>
        private protected T m_InternalValue;

        /// <summary>
        /// The value of the NetworkSerializableVariable container
        /// </summary>
        public virtual T Value => m_InternalValue;

        /// <inheritdoc />
        public override void SetDirty(bool isDirty)
        {
            base.SetDirty(isDirty);

            if (isDirty)
            {
                OnValueChanged?.Invoke(m_InternalValue);
            }
        }

        /// <inheritdoc />
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);

            if (keepDirtyDelta)
            {
                base.SetDirty(true);
            }

            OnValueChanged?.Invoke(m_InternalValue);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader) =>
            m_InternalValue.NetworkSerialize(new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader)));

        /// <inheritdoc />
        public override void WriteDelta(FastBufferWriter writer) => WriteField(writer);

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer) =>
            m_InternalValue.NetworkSerialize(new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer)));
    }
}
