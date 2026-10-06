using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Application.Services;

public sealed class RagPromptBuilder : IRagPromptBuilder
{
    private readonly IConfiguration _configuration;
    private readonly int _maxContextCharacters;

    public RagPromptBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
        _maxContextCharacters = configuration.GetValue<int>("Rag:MaxContextCharacters", 12000);
    }

    public string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> chunks)
    {
        var context = BuildContext(chunks);
        
        return $"""
            Sei un assistente tecnico specializzato nella documentazione. Rispondi alla domanda dell'utente basandoti ESCLUSIVAMENTE sul contesto fornito dalla documentazione tecnica.

            REGOLE IMPORTANTI:
            - Rispondi SEMPRE in italiano, indipendentemente dalla lingua della domanda
            - Rispondi in modo diretto e conciso
            - Usa SOLO le informazioni presenti nel contesto sottostante
            - Se il contesto non contiene informazioni sufficienti per rispondere, dichiaralo chiaramente
            - Cita sezioni o documenti specifici quando possibile
            - Non inventare informazioni e non usare conoscenze esterne
            - Se più documenti forniscono informazioni rilevanti, sintetizzale in modo coerente

            CONTESTO DALLA DOCUMENTAZIONE:
            {context}

            DOMANDA UTENTE:
            {question}

            RISPOSTA (in italiano):
            """;
    }

    private string BuildContext(IReadOnlyList<RetrievedChunk> chunks)
    {
        var contextParts = new List<string>();
        var currentLength = 0;

        foreach (var chunk in chunks)
        {
            var header = $"[Source: {chunk.SourceFile}";
            if (!string.IsNullOrEmpty(chunk.Section))
            {
                header += $" - {chunk.Section}";
            }
            header += $" (Score: {chunk.Score:F3})]";

            var chunkText = $"{header}\n{chunk.Text}\n";
            
            if (currentLength + chunkText.Length > _maxContextCharacters)
            {
                break;
            }

            contextParts.Add(chunkText);
            currentLength += chunkText.Length;
        }

        return string.Join("\n---\n\n", contextParts);
    }
}
