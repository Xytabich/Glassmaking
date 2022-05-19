using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class SimpleToolBehavior : WorkbenchToolItemBehavior
	{
		private AdvancedParticleProperties[] workParticles = null;
		private long tickerId;

		protected bool isUsing = false;

		public SimpleToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(toolCode, blockentity, boundingBoxes)
		{
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				workParticles = slot.Itemstack.Collectible.Attributes?["workbenchParticles"].AsObject<AdvancedParticleProperties[]>(null, slot.Itemstack.Collectible.Code.Domain);

				if(workParticles != null)
				{
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
		}

		public override void OnUnloaded()
		{
			if(Api.Side == EnumAppSide.Client && workParticles != null)
			{
				Blockentity.UnregisterGameTickListener(tickerId);
			}
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(recipe.Steps[step].UseTime.HasValue) isUsing = true;
			return base.OnUseStart(world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			isUsing = false;
			base.OnUseComplete(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			isUsing = false;
			base.OnUseCancel(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		private void SpawnParticles(float dt)
		{
			if(workParticles != null && isUsing)
			{
				foreach(var particle in workParticles)
				{
					Api.World.SpawnParticles(particle);
				}
			}
		}
	}
}