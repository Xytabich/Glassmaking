using ProtoBuf;
using Vintagestory.API.Common;

namespace GlassMaking.GenericItemAction
{
	[ProtoContract]
	public class HeldItemActionMessage
	{
		[ProtoMember(1, IsRequired = true)]
		public int itemId;
		[ProtoMember(2, IsRequired = true, IsPacked = true)]
		public EnumItemClass itemClass;
		[ProtoMember(3)]
		public string action;
		[ProtoMember(4)]
		public byte[] attributes;
	}
}