using enBot.Services.Analysis;

namespace enBot.Tests.Unit;

public class AnalysisServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_FewerThanTwoWords_ReturnsNull()
    {
        var svc = new AnalysisService(new TestCliProcessor(""));
        Assert.Null(await svc.AnalyzeAsync("hello"));
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyString_ReturnsNull()
    {
        var svc = new AnalysisService(new TestCliProcessor(""));
        Assert.Null(await svc.AnalyzeAsync("   "));
    }

    [Fact]
    public async Task AnalyzeAsync_LanguageDash_ReturnsNull()
    {
        var svc = new AnalysisService(new TestCliProcessor("{\"language\":\"--\"}"));
        Assert.Null(await svc.AnalyzeAsync("git commit -m message"));
    }

    [Fact]
    public async Task AnalyzeAsync_NoCorrectionNeeded_ScoreIs10()
    {
        var original = "Hello world this is fine";
        var json = $"{{\"language\":\"en\",\"corrected\":\"\",\"score\":7,\"complexity\":5,\"displayOriginal\":\"{original}\",\"explanations\":[]}}";
        var svc = new AnalysisService(new TestCliProcessor(json));
        var result = await svc.AnalyzeAsync(original);
        Assert.NotNull(result);
        Assert.Equal(10, result.Score);
    }

    [Fact]
    public async Task AnalyzeAsync_WithCorrections_UsesAgentScore()
    {
        var json = "{\"language\":\"en\",\"corrected\":\"I am going home\",\"score\":6,\"complexity\":5,\"displayOriginal\":\"I am going home\",\"explanations\":[\"fixed\"]}";
        var svc = new AnalysisService(new TestCliProcessor(json));
        var result = await svc.AnalyzeAsync("I is going home");
        Assert.NotNull(result);
        Assert.Equal(6, result.Score);
    }

    [Fact]
    public async Task AnalyzeAsync_TextOver600Chars_IsTruncatedBeforeAnalysis()
    {
        var longText = new string('a', 200) + " " + new string('b', 200) + " " + new string('c', 200) + " extra words here";
        var json = "{\"language\":\"en\",\"corrected\":\"\",\"score\":8,\"complexity\":5,\"displayOriginal\":\"\",\"explanations\":[]}";
        var runner = new TestCliProcessor(json);

        await new AnalysisService(runner).AnalyzeAsync(longText);

        Assert.NotNull(runner.CapturedPrompt);
        Assert.DoesNotContain("extra words here", runner.CapturedPrompt);
    }

    [Fact]
    public async Task AnalyzeAsync_NewlinesNormalized()
    {
        var json = "{\"language\":\"en\",\"corrected\":\"\",\"score\":8,\"complexity\":5,\"displayOriginal\":\"\",\"explanations\":[]}";
        var runner = new TestCliProcessor(json);

        await new AnalysisService(runner).AnalyzeAsync("line one\r\nline two\rnewline");

        Assert.NotNull(runner.CapturedPrompt);
        Assert.DoesNotContain("\r\n", runner.CapturedPrompt);
        Assert.DoesNotContain("\r", runner.CapturedPrompt);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidJson_ReturnsNull()
    {
        var svc = new AnalysisService(new TestCliProcessor("not json at all"));
        Assert.Null(await svc.AnalyzeAsync("this is valid text"));
    }

    [Fact]
    public async Task AnalyzeAsync_AgentThrows_ReturnsNull()
    {
        var svc = new AnalysisService(new TestCliProcessor(new InvalidOperationException("claude not found on PATH.")));
        Assert.Null(await svc.AnalyzeAsync("hello there friend"));
    }

    [Fact]
    public async Task AnalyzeAsync_WordCountStoredCorrectly()
    {
        var json = "{\"language\":\"en\",\"corrected\":\"\",\"score\":9,\"complexity\":6,\"displayOriginal\":\"one two three\",\"explanations\":[]}";
        var svc = new AnalysisService(new TestCliProcessor(json));
        var result = await svc.AnalyzeAsync("one two three");
        Assert.NotNull(result);
        Assert.Equal(3, result.WordCount);
    }
}
