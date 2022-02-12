using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public abstract class WorkbenchToolBehavior
	{
		public string ToolCode;

		/// <summary>
		/// The block for this behavior instance.
		/// </summary>
		public BlockEntity Blockentity;

		public ICoreAPI Api;

		public ItemSlot Slot;

		protected Cuboidf[] boundingBoxes;

		public WorkbenchToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes)
		{
			ToolCode = toolCode.ToLowerInvariant();
			Blockentity = blockentity;
			this.boundingBoxes = boundingBoxes;
		}

		/// <summary>
		/// Called right after the tool behavior was created
		/// </summary>
		public virtual void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			Api = api;
			Slot = slot;
		}

		public virtual void OnUnloaded()
		{

		}

		public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			return false;
		}

		public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
		}

		public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			return false;
		}

		public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
		{
			handling = EnumHandling.PassThrough;
			return false;
		}

		public virtual WorldInteraction[] GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return null;
		}

		public virtual ITreeAttribute ToAttribute()
		{
			return null;
		}

		public virtual void FromAttribute(IAttribute tree, IWorldAccessor worldAccessForResolve)
		{
		}

		public virtual Cuboidf[] GetBoundingBoxes()
		{
			return boundingBoxes;
		}

		public virtual void OnBlockRemoved()
		{
		}

		public virtual void OnBlockUnloaded()
		{
		}
	}
}