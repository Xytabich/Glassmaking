namespace GlassMaking.Blocks
{
	public interface IWorkbenchRenderInfo
	{
		IWorkpieceRenderer workpieceRenderer { get; }
	}

	public interface IWorkpieceRenderer
	{
		float[] itemTransform { get; }
	}
}