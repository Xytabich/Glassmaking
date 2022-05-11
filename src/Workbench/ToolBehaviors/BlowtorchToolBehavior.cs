using GlassMaking.Items;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class BlowtorchToolBehavior : WorkbenchToolItemBehavior
	{
		public const string CODE = "blowtorch";

		private AdvancedParticleProperties[] workParticles = null;
		private long tickerId;
		private bool isUsing = false;

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

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue)
			{
				var item = (ItemBlowtorch)Slot.Itemstack.Item;
				if(recipe.Steps[step].Tools[CODE]["temperature"].AsFloat() <= item.flameTemperature)
				{
					isUsing = true;
					var useLitres = item.consumptionPerSecond;
					return item.GetCurrentLitres(Slot.Itemstack) >= useLitres * useTime.Value;
				}
				else return false;
			}
			return base.OnUseStart(world, byPlayer, blockSel, recipe, step);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue)
			{
				var item = (ItemBlowtorch)Slot.Itemstack.Item;
				if(recipe.Steps[step].Tools[CODE]["temperature"].AsFloat() <= item.flameTemperature)
				{
					var useLitres = item.consumptionPerSecond;
					return item.GetCurrentLitres(Slot.Itemstack) >= useLitres * useTime.Value;
				}
				else return false;
			}
			return base.OnUseStep(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			isUsing = false;
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue)
			{
				var item = (ItemBlowtorch)Slot.Itemstack.Item;
				var useLitres = item.consumptionPerSecond;
				item.TryTakeLiquid(Slot.Itemstack, useLitres * useTime.Value);
				Slot.MarkDirty();
			}
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