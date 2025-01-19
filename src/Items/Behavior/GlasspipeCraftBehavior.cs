using Vintagestory.API.Common;

namespace GlassMaking.Items.Behavior
{
	public abstract class GlasspipeCraftBehavior : CollectibleBehavior, IPrioritizedBehavior
	{
		/// <inheritdoc/>
		public abstract double Priority { get; }

		protected ICoreAPI api = default!;
		protected GlassMakingMod glassMaking = default!;
		protected readonly ItemGlassworkPipe glassworkPipe;

		public GlasspipeCraftBehavior(CollectibleObject collObj) : base(collObj)
		{
			glassworkPipe = (ItemGlassworkPipe)collObj;
		}

		public override void OnLoaded(ICoreAPI api)
		{
			this.api = api;
			glassMaking = api.ModLoader.GetModSystem<GlassMakingMod>();
		}

		/// <summary>
		/// Returns true if the behavior is currently active and should take priority over all actions
		/// </summary>
		public abstract bool IsActive(ItemStack itemStack);

		/// <summary>
		/// Returns true if the workpiece temperature is above the critical temperature at which the workpiece is considered defective
		/// </summary>
		public abstract bool IsHeated(IWorldAccessor world, ItemStack itemStack);

		/// <summary>
		/// Returns true if the workpiece has sufficient temperature for work
		/// </summary>
		public abstract bool IsWorkingTemperature(IWorldAccessor world, ItemStack item);
	}
}