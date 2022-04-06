using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class BlowtorchToolBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "blowtorch";

		private AdvancedParticleProperties[] workParticles = null;
		private long tickerId;

		public BlowtorchToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				workParticles = slot.Itemstack.Collectible.Attributes?["workbenchParticles"].AsObject<AdvancedParticleProperties[]>(null, slot.Itemstack.Collectible.Code.Domain);

				var pos = Blockentity.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
				var face = BlockFacing.FromCode(Blockentity.Block.Variant["side"]);
				foreach(var p in workParticles)
				{
					p.basePos = pos;
					p.PosOffset = Utils.RotateHorizontal(face, p.PosOffset);
					p.Velocity = Utils.RotateHorizontal(face, p.Velocity);
				}
				tickerId = Blockentity.RegisterGameTickListener(SpawnParticles, 100);
			}
		}

		public override void OnUnloaded()
		{
			if(Api.Side == EnumAppSide.Client)
			{
				Blockentity.UnregisterGameTickListener(tickerId);
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