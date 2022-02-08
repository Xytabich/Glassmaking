namespace GlassMaking.Blocks
{
    public interface IBurnerModifier
    {
        float durationModifier { get; }
        float temperatureModifier { get; }
    }
}