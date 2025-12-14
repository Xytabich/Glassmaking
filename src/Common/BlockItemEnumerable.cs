using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
	public static class BlockItemEnumerableExt
	{
		public static BlockItemEnumerable BlockItemEnumerator(this IWorldAccessor world)
		{
			return new(world);
		}
	}

	public readonly struct BlockItemEnumerable : IEnumerable<CollectibleObject>
	{
		private readonly IWorldAccessor world;

		public BlockItemEnumerable(IWorldAccessor world)
		{
			this.world = world;
		}

		public BlockItemEnumerator GetEnumerator()
		{
			return new BlockItemEnumerator(world);
		}

		IEnumerator<CollectibleObject> IEnumerable<CollectibleObject>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public struct BlockItemEnumerator : IEnumerator<CollectibleObject>
	{
		public readonly CollectibleObject Current => listSelect ? items[index] : blocks[index];

		readonly object IEnumerator.Current => Current;

		private int index, count;
		private bool listSelect;
		private readonly IList<Block> blocks;
		private readonly IList<Item> items;

		public BlockItemEnumerator(IWorldAccessor world)
		{
			blocks = world.Blocks;
			items = world.Items;
			index = -1;
			count = blocks.Count;
		}

		public bool MoveNext()
		{
			index++;
			if(index == count && !listSelect)
			{
				listSelect = true;
				index = 0;
				count = items.Count;
			}

			return index < count;
		}

		public void Reset()
		{
			listSelect = false;
			index = -1;
			count = blocks.Count;
		}

		public readonly void Dispose()
		{
		}
	}
}