using RagPlatform.Application.Common.Models;

namespace RagPlatform.Application.Common.Interfaces;

/// <summary>
/// Extracts text from an uploaded file and splits it into overlapping chunks.
/// </summary>
public interface IDocumentProcessingService
{
    ExtractedDocument Process(Stream fileStream, string fileName);
}
