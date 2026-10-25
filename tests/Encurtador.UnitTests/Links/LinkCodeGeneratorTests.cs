using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSevenCharactersFromConfiguredAlphabet()
    {
        var generator = new LinkCodeGenerator();

        var code = generator.Generate();

        Assert.Equal(7, code.Length);
        Assert.All(code, character => Assert.Contains(character, LinkCodeGenerator.Alphabet));
    }
}
