using ProtoBuf;
using Vintagestory.API.Common;

namespace GlassMaking.GenericItemAction
{
	[ProtoContract]
	public class HeldItemActionMessage
	{
		[ProtoMember(1, IsRequired = true)]
		public int ItemId;
		[ProtoMember(2, IsRequired = true, IsPacked = true)]
		public EnumItemClass ItemClass;
		[ProtoMember(3)]
		public string Action = default!;
		[ProtoMember(4)]
		public byte[]? Attributes;
	}
}