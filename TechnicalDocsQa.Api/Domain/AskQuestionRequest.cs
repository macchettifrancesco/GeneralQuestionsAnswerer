using System.ComponentModel.DataAnnotations;

namespace TechnicalDocsQa.Api.Domain;

public sealed record AskQuestionRequest(
    [Required]
    [MinLength(3)]
    [MaxLength(500)]
    string Question);
