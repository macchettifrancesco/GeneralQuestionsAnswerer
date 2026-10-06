using System.Text;
using System.Text.RegularExpressions;
using TechnicalDocsQa.Api.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class MarkdownParserService : IMarkdownParserService
{
    private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private readonly ILogger<MarkdownParserService> _logger;

    public MarkdownParserService(ILogger<MarkdownParserService> logger)
    {
        _logger = logger;
    }

    public ParsedMarkdown Parse(string filePath, string sourceFile, string sourcePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        _logger.LogDebug("Parsing document from {SourceFile} with extension {Extension}", sourceFile, extension);

        return extension switch
        {
            ".md" => ParseMarkdown(File.ReadAllText(filePath), sourceFile, sourcePath),
            ".txt" => ParseTextFile(File.ReadAllText(filePath), sourceFile, sourcePath),
            ".pdf" => ParsePdfFile(filePath, sourceFile, sourcePath),
            _ => throw new NotSupportedException($"File extension {extension} is not supported")
        };
    }

    private ParsedMarkdown ParseMarkdown(string markdown, string sourceFile, string sourcePath)
    {
        _logger.LogDebug("Parsing markdown from {SourceFile}", sourceFile);

        // Strip YAML frontmatter
        markdown = StripYamlFrontmatter(markdown);

        // Extract title
        var title = ExtractTitle(markdown, sourceFile);

        // Split into sections
        var sections = ParseSections(markdown);

        // Clean the full text
        var cleanText = CleanText(markdown);

        _logger.LogDebug("Parsed markdown: Title='{Title}', Sections={SectionCount}, Length={Length}", 
            title, sections.Count, cleanText.Length);

        return new ParsedMarkdown
        {
            Title = title,
            CleanText = cleanText,
            Sections = sections
        };
    }

    private ParsedMarkdown ParseTextFile(string content, string sourceFile, string sourcePath)
    {
        _logger.LogDebug("Parsing text file from {SourceFile}", sourceFile);

        var title = ExtractTitleFromFilename(sourceFile);
        var cleanText = CleanText(content);

        // Split text into sections by paragraphs (double line break)
        var sections = new List<MarkdownSection>();
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var (para, index) in paragraphs.Select((p, i) => (p, i)))
        {
            if (string.IsNullOrWhiteSpace(para)) continue;
            
            sections.Add(new MarkdownSection
            {
                Heading = $"Section {index + 1}",
                Level = 1,
                Content = para.Trim(),
                StartIndex = 0
            });
        }

        _logger.LogDebug("Parsed text file: Title='{Title}', Sections={SectionCount}, Length={Length}", 
            title, sections.Count, cleanText.Length);

        return new ParsedMarkdown
        {
            Title = title,
            CleanText = cleanText,
            Sections = sections
        };
    }

    private ParsedMarkdown ParsePdfFile(string filePath, string sourceFile, string sourcePath)
    {
        _logger.LogDebug("Parsing PDF from {SourceFile}", sourceFile);

        var sb = new StringBuilder();
        
        try
        {
            using var document = PdfDocument.Open(filePath);
            
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                sb.AppendLine(text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PDF {SourceFile}", sourceFile);
            throw new InvalidOperationException($"Failed to parse PDF: {ex.Message}", ex);
        }

        var content = sb.ToString();
        var title = ExtractTitleFromFilename(sourceFile);
        var cleanText = CleanText(content);

        // Split by pages or large text blocks
        var sections = new List<MarkdownSection>
        {
            new MarkdownSection
            {
                Heading = "PDF Content",
                Level = 1,
                Content = cleanText,
                StartIndex = 0
            }
        };

        _logger.LogDebug("Parsed PDF: Title='{Title}', Sections={SectionCount}, Length={Length}", 
            title, sections.Count, cleanText.Length);

        return new ParsedMarkdown
        {
            Title = title,
            CleanText = cleanText,
            Sections = sections
        };
    }

    private static string ExtractTitleFromFilename(string filename)
    {
        return Path.GetFileNameWithoutExtension(filename)
            .Replace('_', ' ')
            .Replace('-', ' ');
    }

    private static string StripYamlFrontmatter(string markdown)
    {
        if (markdown.StartsWith("---", StringComparison.Ordinal))
        {
            var endIndex = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (endIndex >= 0)
            {
                markdown = markdown[(endIndex + 4)..].TrimStart();
            }
        }
        return markdown;
    }

    private static string ExtractTitle(string markdown, string sourceFile)
    {
        var match = HeadingRegex.Match(markdown);
        if (match.Success && match.Groups[1].Value == "#")
        {
            return match.Groups[2].Value.Trim();
        }

        // Derive from source file name
        return Path.GetFileNameWithoutExtension(sourceFile);
    }

    private static IReadOnlyList<MarkdownSection> ParseSections(string markdown)
    {
        var sections = new List<MarkdownSection>();
        var matches = HeadingRegex.Matches(markdown);

        if (matches.Count == 0)
        {
            return sections;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var heading = match.Groups[2].Value.Trim();
            var level = match.Groups[1].Value.Length;
            var startIndex = match.Index;
            
            // Content starts after the heading line
            var contentStart = match.Index + match.Length;
            
            // Content ends at the next heading or end of document
            var contentEnd = i < matches.Count - 1 
                ? matches[i + 1].Index 
                : markdown.Length;

            var content = markdown[contentStart..contentEnd].Trim();

            sections.Add(new MarkdownSection
            {
                Heading = heading,
                Level = level,
                Content = content,
                StartIndex = startIndex
            });
        }

        return sections;
    }

    private static string CleanText(string text)
    {
        // Normalize multiple blank lines to single blank line
        text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");
        
        // Trim leading and trailing whitespace
        return text.Trim();
    }
}
