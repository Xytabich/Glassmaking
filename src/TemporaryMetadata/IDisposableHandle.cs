namespace GlassMaking.TemporaryMetadata
{
    public interface IDisposableHandle
    {
        /// <summary>
        /// Removes object from pool
        /// </summary>
        void Dispose();

        /// <summary>
        /// Postpones disposal of an object
        /// </summary>
        void Postpone();
    }
}