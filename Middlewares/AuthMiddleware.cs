using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TodoAPI.Data;
using TodoAPI.Methods;

namespace TodoAPI.Middleware
{
  public class AuthMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public AuthMiddleware(RequestDelegate next, IConfiguration config)
    {
      _next = next;
      _config = config;
    }

    public async Task Invoke(HttpContext httpContext, DataContext dataContext)
    {
      string[] ignorePath = {"/api/Auth/register", "/api/Auth/login" };

      if(!ignorePath.Contains(httpContext.Request.Path.Value)) {
        await Validate(httpContext, dataContext);
      }

      await _next(httpContext);
    }

    private async Task Validate(HttpContext httpContext, DataContext dataContext)
    {
      httpContext.Response.Clear();
      httpContext.Response.StatusCode = 400;

      var accessToken = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

      if(accessToken == null) {
        await httpContext.Response.WriteAsync("Missing access token!");
        return;
      }

      var validatedToken = AuthMethod.ValidateJSONWebToken(accessToken, _config["Jwt:AccessTokenSecret"], _config["Jwt:Issuer"], _config["Jwt:Audience"]);
      
      if (validatedToken == null)
      {
        await httpContext.Response.WriteAsync("Incorrect access token!");
        return;
      }

      httpContext.Items["user"] = await dataContext.Users.FindAsync(validatedToken);
    }
  }
}