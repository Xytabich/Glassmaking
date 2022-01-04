using ProtoBuf;

namespace GlassMaking.GenericItemAction
{
    [ProtoContract]
    public class HeldItemActionMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public int itemId;
        [ProtoMember(2)]
        public string action;
        [ProtoMember(3)]
        public byte[] attributes;
    }
}