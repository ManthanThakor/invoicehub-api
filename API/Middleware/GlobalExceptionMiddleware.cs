using System.Net;
using System.Text.Json;
using Application.DTOs;
using InvoiceHub.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InvoiceHub.API.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a clean ApiResponse JSON.
/// Prevents stack traces from leaking to clients in production.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate               _next;
    private readonly ILogger<GlobalExceptionMiddleware> _log;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> log)
    {
        _next = next;
        _log  = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.LogWarning(ex, "Unauthorized access: {Path}", ctx.Request.Path);
            await WriteErrorAsync(ctx, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _log.LogWarning(ex, "Resource not found: {Path}", ctx.Request.Path);
            await WriteErrorAsync(ctx, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _log.LogWarning(ex, "Bad argument: {Path}", ctx.Request.Path);
            await WriteErrorAsync(ctx, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogWarning(ex, "Invalid operation: {Path}", ctx.Request.Path);
            await WriteErrorAsync(ctx, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            // Log full exception — do NOT expose to client
            _log.LogError(ex,
                "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            await WriteErrorAsync(ctx, HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again or contact support.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext ctx, HttpStatusCode status, string message)
    {
        ctx.Response.StatusCode  = (int)status;
        ctx.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message);
        var json     = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null   // keep PascalCase consistent with API
        });

        await ctx.Response.WriteAsync(json);
    }
}