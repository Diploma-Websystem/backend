using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUtilities.Application.Interfaces;

namespace WebUtilities.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilitiesController : ControllerBase
{
    private readonly IQrCodeService _qrCodeService;
    private readonly IUrlShortenerService _urlShortenerService;
    private readonly IJsonFormatterService _jsonFormatterService;
    private readonly IIpAnalyzerService _ipAnalyzerService;

    public UtilitiesController(
        IQrCodeService qrCodeService,
        IUrlShortenerService urlShortenerService,
        IJsonFormatterService jsonFormatterService,
        IIpAnalyzerService ipAnalyzerService)
    {
        _qrCodeService = qrCodeService;
        _urlShortenerService = urlShortenerService;
        _jsonFormatterService = jsonFormatterService;
        _ipAnalyzerService = ipAnalyzerService;
    }

    [HttpPost("qr")]
    [AllowAnonymous]
    public IActionResult GenerateQr([FromBody] QrRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Text is required." });
        }

        var bytes = _qrCodeService.GeneratePng(request.Text);
        return File(bytes, "image/png", "qr.png");
    }

    [HttpPost("shorten")]
    [AllowAnonymous]
    public async Task<IActionResult> ShortenUrl([FromBody] ShortenUrlRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { message = "Url is required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try
        {
            var code = await _urlShortenerService.CreateShortCodeAsync(request.Url, userId, cancellationToken);
            var redirectUrl = $"{Request.Scheme}://{Request.Host}/api/utilities/r/{code}";
            return Ok(new { shortCode = code, shortUrl = redirectUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("r/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> RedirectToOriginal([FromRoute] string code, CancellationToken cancellationToken)
    {
        var originalUrl = await _urlShortenerService.GetOriginalUrlByCodeAsync(code, cancellationToken);
        if (originalUrl is null)
        {
            return NotFound(new { message = "Short code not found." });
        }

        return Redirect(originalUrl);
    }

    [HttpPost("json")]
    [AllowAnonymous]
    public IActionResult ProcessJson([FromBody] JsonProcessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Json))
        {
            return BadRequest(new { message = "Json payload is required." });
        }

        var action = request.Action?.Trim().ToLowerInvariant();
        if (action is not ("format" or "minify"))
        {
            return BadRequest(new { message = "Action must be either 'format' or 'minify'." });
        }

        try
        {
            var result = _jsonFormatterService.Process(request.Json, minify: action == "minify");
            return Ok(new { result });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { message = "Invalid JSON.", details = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("ip")]
    [AllowAnonymous]
    public async Task<IActionResult> AnalyzeIp([FromQuery] string? ip, CancellationToken cancellationToken)
    {
        var ipAddress = ip;
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        var result = await _ipAnalyzerService.AnalyzeAsync(ipAddress, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    public sealed class QrRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public sealed class ShortenUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public sealed class JsonProcessRequest
    {
        public string Json { get; set; } = string.Empty;
        public string Action { get; set; } = "format";
    }
}
