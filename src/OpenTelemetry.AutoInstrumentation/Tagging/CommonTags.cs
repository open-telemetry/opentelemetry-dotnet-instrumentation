namespace OpenTelemetry.AutoInstrumentation.Tagging;

internal class CommonTags : TagsList
{
    protected static readonly IProperty<string>[] CommonTagsProperties =
    {
        new Property<CommonTags, string>(Tags.Env, t => t.Environment, (t, v) => t.Environment = v),
        new Property<CommonTags, string>(Tags.Version, t => t.Version, (t, v) => t.Version = v)
    };

    public string Environment { get; set; }

    public string Version { get; set; }

    protected override IProperty<string>[] GetAdditionalTags() => CommonTagsProperties;
}
