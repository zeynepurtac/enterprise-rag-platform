using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Application.Common.Models;
using RagPlatform.Domain.Enums;
using RagPlatform.Infrastructure.Options;

namespace RagPlatform.Infrastructure.Llm;

/// <summary>
/// Chat client for the OpenAI-compatible <c>/chat/completions</c> endpoint,
/// supporting both a buffered call and token streaming (SSE). Works against
/// Ollama (<c>/v1/chat/completions</c>) and OpenAI without code changes.
/// </summary>
public sealed class OpenAiCompatibleChatCompletionService : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiCompatibleChatCompletionService(HttpClient http, IOptions<AiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> history,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(systemPrompt, history, userPrompt, stream: false);

        using var response = await _http.PostAsJsonAsync("chat/completions", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Chat completion failed ({(int)response.StatusCode}): {error}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<ChatResponse>(
            JsonOptions, cancellationToken);

        return parsed?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatTurn> history,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(systemPrompt, history, userPrompt, stream: true);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _http.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Chat stream failed ({(int)response.StatusCode}): {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                yield break;
            }

            string? token = null;
            try
            {
                var chunk = JsonSerializer.Deserialize<ChatResponse>(data, JsonOptions);
                token = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            }
            catch (JsonException)
            {
                // Ignore malformed keep-alive fragments.
            }

            if (!string.IsNullOrEmpty(token))
            {
                yield return token;
            }
        }
    }

    private ChatRequest BuildRequest(
        string systemPrompt,
        IReadOnlyList<ChatTurn> history,
        string userPrompt,
        bool stream)
    {
        var messages = new List<ChatMessagePayload>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        foreach (var turn in history)
        {
            messages.Add(new ChatMessagePayload
            {
                Role = turn.Role == ChatRole.Assistant ? "assistant" : "user",
                Content = turn.Content
            });
        }

        messages.Add(new ChatMessagePayload { Role = "user", Content = userPrompt });

        return new ChatRequest
        {
            Model = _options.ChatModel,
            Stream = stream,
            Temperature = 0.2,
            Messages = messages
        };
    }

    private sealed class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessagePayload> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private sealed class ChatMessagePayload
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessagePayload? Message { get; set; }

        [JsonPropertyName("delta")]
        public ChatMessagePayload? Delta { get; set; }
    }
}
