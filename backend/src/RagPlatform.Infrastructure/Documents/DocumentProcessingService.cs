using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Infrastructure.Options;


using LocationTextExtractionStrategy = iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy;
using ITextTextChunk = iText.Kernel.Pdf.Canvas.Parser.Listener.TextChunk;

namespace RagPlatform.Infrastructure.Documents;

/// <summary>
/// Extracts text from PDF files using iText 7 and splits it into overlapping,
/// token-bounded chunks. Chunking happens per page so that citations can point
/// back to a concrete page number.
/// </summary>
public sealed partial class DocumentProcessingService : IDocumentProcessingService
{
    // Rough heuristic: ~4 characters per token for English/Latin text.
    private const double CharsPerToken = 4.0;

    private readonly ChunkingOptions _options;

    public DocumentProcessingService(IOptions<ChunkingOptions> options)
    {
        _options = options.Value;
    }

    public ExtractedDocument Process(Stream fileStream, string fileName)
    {
        // iText reads from a seekable stream; copy into memory to be safe.
        using var buffer = new MemoryStream();
        fileStream.CopyTo(buffer);
        buffer.Position = 0;

        var chunks = new List<TextChunk>();
        var maxChars = (int)Math.Round(_options.MaxTokens * CharsPerToken);
        var overlapChars = (int)Math.Round(_options.OverlapTokens * CharsPerToken);
        var chunkIndex = 0;
        int pageCount;

        using (var reader = new PdfReader(buffer))
        using (var pdf = new PdfDocument(reader))
        {
            pageCount = pdf.GetNumberOfPages();

            for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                var page = pdf.GetPage(pageNumber);
                var strategy = new LocationTextExtractionStrategy();
                var rawText = PdfTextExtractor.GetTextFromPage(page, strategy);

                var normalized = NormalizeWhitespace(rawText);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                foreach (var slice in SplitIntoWindows(normalized, maxChars, overlapChars))
                {
                    var tokenCount = (int)Math.Ceiling(slice.Length / CharsPerToken);
                    chunks.Add(new TextChunk(chunkIndex++, pageNumber, slice, tokenCount));
                }
            }
        }

        return new ExtractedDocument(pageCount, chunks);
    }

    /// <summary>
    /// Splits a block of text into windows of at most <paramref name="maxChars"/>
    /// characters, breaking on word boundaries and carrying an overlap forward.
    /// </summary>
    private static IEnumerable<string> SplitIntoWindows(string text, int maxChars, int overlapChars)
    {
        if (text.Length <= maxChars)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < text.Length)
        {
            var end = Math.Min(start + maxChars, text.Length);

            // Prefer to break on the last whitespace within the window so we do
            // not cut words in half.
            if (end < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', end - 1, end - start);
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            var slice = text[start..end].Trim();
            if (slice.Length > 0)
            {
                yield return slice;
            }

            if (end >= text.Length)
            {
                yield break;
            }

            // Move the window forward, keeping the configured overlap.
            var nextStart = end - overlapChars;
            start = nextStart <= start ? end : nextStart;
        }
    }

    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var collapsed = WhitespaceRegex().Replace(input, " ");
        return collapsed.Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
