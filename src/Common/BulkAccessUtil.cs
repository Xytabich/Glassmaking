using Vintagestory.API.Common;

namespace GlassMaking
{
	internal readonly struct BulkAccessUtil
	{
		private readonly IBulkBlockAccessor blockAccessor;
		private readonly bool readFromStagedByDefault;

		private BulkAccessUtil(IBulkBlockAccessor blockAccessor, bool readFromStagedByDefault)
		{
			this.blockAccessor = blockAccessor;
			this.readFromStagedByDefault = readFromStagedByDefault;
		}

		public readonly void RollbackValue()
		{
			blockAccessor.ReadFromStagedByDefault = readFromStagedByDefault;
		}

		public static BulkAccessUtil SetReadFromStagedByDefault(IBulkBlockAccessor blockAccessor, bool value)
		{
			var handle = new BulkAccessUtil(blockAccessor, blockAccessor.ReadFromStagedByDefault);
			blockAccessor.ReadFromStagedByDefault = value;
			return handle;
		}
	}
}