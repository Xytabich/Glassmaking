using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class BlowtorchToolBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "blowtorch";

		private AdvancedParticleProperties[] workParticles = null;

		public BlowtorchToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				workParticles = slot.Itemstack.Collectible.Attributes?["workbenchParticles"].AsObject<AdvancedParticleProperties[]>(null, slot.Itemstack.Collectible.Code.Domain);
				Blockentity.RegisterGameTickListener(SpawnParticles, 100);
			}
		}

		private void SpawnParticles(float dt)
		{
			if(workParticles != null)
			{
				foreach(var particle in workParticles)
				{
					Api.World.SpawnParticles(particle);
				}
			}
		}
	}
}