namespace Encurtador.Application.Links;

public sealed class Link
{
    private Link()
    {
        Code = string.Empty;
        DestinationUrl = string.Empty;
    }

    public Link(string code, string destinationUrl)
    {
        Code = code;
        DestinationUrl = destinationUrl;
    }

    public long Id { get; private set; }

    public string Code { get; private set; }

    public string DestinationUrl { get; private set; }

    public long ClickCount { get; private set; }
}
