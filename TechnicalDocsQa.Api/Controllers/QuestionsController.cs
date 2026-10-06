using Microsoft.AspNetCore.Mvc;
using TechnicalDocsQa.Api.Application.Interfaces;
using TechnicalDocsQa.Api.Domain;

namespace TechnicalDocsQa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionAnsweringService _questionAnsweringService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionAnsweringService questionAnsweringService,
        ILogger<QuestionsController> logger)
    {
        _questionAnsweringService = questionAnsweringService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question about the indexed technical documentation
    /// </summary>
    /// <param name="request">The question to ask</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer with sources from the documentation</returns>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskQuestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AskQuestionResponse>> AskQuestion(
        [FromBody] AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _questionAnsweringService.AnswerQuestionAsync(
                request.Question, 
                cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error answering question: {Question}", request.Question);
            return StatusCode(500, new { error = "Failed to answer question", detail = ex.Message });
        }
    }
}
