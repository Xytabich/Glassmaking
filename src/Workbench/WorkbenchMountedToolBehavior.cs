using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	/// <summary>
	/// Describes the behavior of a tool mounted on a workbench.
	/// </summary>
	public abstract class WorkbenchMountedToolBehavior : WorkbenchToolBehavior
	{
		public override string ToolCode { get; }

		/// <summary>
		/// The block for this behavior instance.
		/// </summary>
		public BlockEntity Blockentity;

		public ItemSlot Slot = default!;

		protected Cuboidf[] boundingBoxes;

		public WorkbenchMountedToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes)
		{
			this.ToolCode = toolCode.ToLowerInvariant();
			Blockentity = blockentity;
			this.boundingBoxes = boundingBoxes;
		}

		/// <summary>
		/// Called right after the tool behavior was created
		/// </summary>
		public virtual void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api);
			Slot = slot;
		}

		/// <summary>
		/// Called if this tool is being used in the current step of the recipe, but crafting is idle (i.e. the player is not interacting with the workbench).
		/// Called when a craft is canceled, when a tool is placed, or when a recipe is selected. Only on client-side.
		/// </summary>
		public virtual void OnIdleStart(IWorldAccessor world, WorkbenchRecipe recipe, int step)
		{
		}

		/// <summary>
		/// Called if crafting has been started or the recipe has changed.
		/// </summary>
		/// <param name="recipe">Current recipe value, may be null</param>
		/// <param name="step">Current recipe step</param>
		public virtual void OnIdleStop(IWorldAccessor world, WorkbenchRecipe? recipe, int step)
		{
		}

		public virtual IAttribute? ToAttribute()
		{
			return null;
		}

		public virtual void FromAttribute(IAttribute? attribute, IWorldAccessor worldAccessForResolve)
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