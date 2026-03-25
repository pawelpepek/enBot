using enBot.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace enBot.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    public List<InlineSegment> OriginalInlines { get; }
    public List<InlineSegment> CorrectedInlines { get; }
    public int Score { get; }
    public string ScoreText { get; }
    public int Complexity { get; }
    public string ComplexityText { get; }
    public string Language { get; }
    public List<List<InlineSegment>> Explanations { get; }
    public bool HasExplanations { get; }

    public NotificationViewModel(HookPayload payload)
    {
        var displayOriginal = string.IsNullOrEmpty(payload.DisplayOriginal) ? payload.Original : payload.DisplayOriginal;
        OriginalInlines = ParseCorrected(displayOriginal);
        CorrectedInlines = ParseCorrected(payload.Corrected);
        Score = payload.Score;
        ScoreText = $"{payload.Score}/10";
        Complexity = payload.Complexity;
        ComplexityText = $"C {payload.Complexity}/10";
        Language = "EN";
        Explanations = (payload.Explanations ?? []).ConvertAll(ParseCorrected);
        HasExplanations = Explanations.Count > 0;
    }

    private static List<InlineSegment> ParseCorrected(string text)
    {
        var segments = new List<InlineSegment>();
        var pattern = @"\*\*(.+?)\*\*|\*(?!\*)(.+?)(?<!\*)\*";
        var lastIndex = 0;

        text = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

        foreach (Match match in Regex.Matches(text, pattern))
        {
            if (match.Index > lastIndex)
                AddWords(segments, text[lastIndex..match.Index]);

            if (match.Value.StartsWith("**"))
                segments.Add(new InlineSegment(match.Groups[1].Value + " ", IsBold: true));
            else
                segments.Add(new InlineSegment(match.Groups[2].Value + " ", IsBold: false, IsItalic: true));

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            AddWords(segments, text[lastIndex..]);

        return segments;
    }

    private static void AddWords(List<InlineSegment> segments, string text)
    {
        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            var word = i < words.Length - 1 ? words[i] + " " : words[i];
            if (!string.IsNullOrEmpty(word))
                segments.Add(new InlineSegment(word, false));
        }
    }
}
