using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public interface IGlassmeltSource
	{
		bool CanInteract(EntityAgent byEntity, BlockSelection blockSel);

		float GetTemperature();

		int GetGlassAmount();

		AssetLocation GetGlassCode();

		void RemoveGlass(int amount);

		void SpawnMeltParticles(IWorldAccessor world, BlockSelection blockSel, IPlayer byPlayer, float quantity = 1f);
	}
}