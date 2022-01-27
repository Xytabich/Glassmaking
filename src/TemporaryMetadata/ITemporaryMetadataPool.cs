using System;

namespace GlassMaking.TemporaryMetadata
{
    public interface ITemporaryMetadataPool<T> where T : IDisposable
    {
        IDisposableHandle AllocateHandle(T value);
    }
}