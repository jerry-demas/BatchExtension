using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace CBIZ.CCH.BatchExtension.API.Middelware;

public class ErrorHandlingMiddleware
{
 private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
       try
        {    
            await _next(context);
            
            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                !context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "The requested endpoint does not exist.",
                    status = 404
                });
            }
        }
        catch (Exception ex)
        {          
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                message = ex.Message
            });
        }
    }
}
