using Vintagestory.API.Client;

namespace GlassMaking.Blocks
{
	public interface IWorkbenchRenderInfo
	{
		IWorkpieceRenderer workpieceRenderer { get; }
	}

	public interface IWorkpieceRenderer
	{
		Matrixf itemTransform { get; }
	}
}