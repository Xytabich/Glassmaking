using Vintagestory.API.Client;

namespace GlassMaking.Blocks
{
	public interface IWorkbenchRenderInfo
	{
		IWorkpieceRenderer WorkpieceRenderer { get; }
	}

	public interface IWorkpieceRenderer
	{
		Matrixf ItemTransform { get; }
	}
}