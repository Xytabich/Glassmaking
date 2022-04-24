using GlassMaking.Blocks.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LatheToolBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "lathe";

		private AnimatorBase animator;
		private BlockAnimatableRenderer renderer;

		public LatheToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				var capi = (ICoreClientAPI)api;
				AnimUtil.GetAnimatableMesh(capi, slot.Itemstack.Collectible, (ITexPositionSource)Blockentity, out var meshRef, out var shape);
				animator = BlockEntityAnimationUtil.GetAnimator(api, "glassmaking:lathe|" + slot.Itemstack.Collectible.Code.ToString(), shape);

				ModelTransform transform = slot.Itemstack.Collectible.Attributes?["workbenchToolTransform"].AsObject<ModelTransform>().EnsureDefaultValues();
				renderer = new BlockAnimatableRenderer(capi, Blockentity.Pos, new Vec3f(0, Blockentity.Block.Shape.rotateY, 0), transform, animator, meshRef, false);
			}
		}

		public override void OnUnloaded()
		{
			base.OnUnloaded();
			renderer?.Dispose();
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			renderer?.Dispose();
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			renderer?.Dispose();
		}
	}
}