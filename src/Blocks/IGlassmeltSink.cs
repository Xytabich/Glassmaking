using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public interface IGlassmeltSink
	{
		bool IsLiquid { get; }

		bool CanReceiveGlass(AssetLocation code, int amount);

		void ReceiveGlass(EntityAgent byEntity, AssetLocation code, ref int amount, float temperature);

		void OnPourOver();
	}
}