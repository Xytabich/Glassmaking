using Vintagestory.API.Common;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructurePlanMain : BlockHorizontalStructureMain
	{
		public int requiredSurrogates = 0;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] != null && structure[x, y, z].Id != Id)
						{
							if(!(structure[x, y, z] is IStructurePlanOptionalBlock))
							{
								requiredSurrogates++;
							}
						}
					}
				}
			}
		}
	}
}