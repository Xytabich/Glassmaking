namespace GlassMaking.Items.Behavior
{
	/// <summary>
	/// Used to indicate the priority of behavior function calls
	/// </summary>
	public interface IPrioritizedBehavior
	{
		/// <summary>
		/// Behavior priority - the higher the number, the higher the priority
		/// </summary>
		double Priority { get; }
	}
}