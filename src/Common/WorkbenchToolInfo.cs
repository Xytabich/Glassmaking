using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
	[JsonObject]
	public class WorkbenchToolInfo
	{
		public AssetLocation code;
		public JsonObject attributes;
	}
}