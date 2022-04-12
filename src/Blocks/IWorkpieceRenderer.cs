using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public interface IWorkbenchRenderInfo
	{
		IWorkpieceRenderer workpieceRenderer { get; }
	}

	public interface IWorkpieceRenderer
	{
		ModelTransform itemTransform { get; }
	}
}