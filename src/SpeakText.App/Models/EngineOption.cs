namespace SpeakText.App.Models;

public sealed class EngineOption
{
    public EngineOption(string id, string label)
    {
        Id = id;
        Label = label;
    }

    public string Id { get; }

    public string Label { get; }
}
