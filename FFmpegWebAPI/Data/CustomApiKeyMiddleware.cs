using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace FFmpegWebAPI.Data
{
    public class CustomApiKeyMiddleware
    {
        private readonly IConfiguration Configuration;
        private readonly RequestDelegate _next;
        const string API_KEY = "Api_Key";
        public CustomApiKeyMiddleware(RequestDelegate next,
        IConfiguration configuration)
        {
            _next = next;
            Configuration = configuration;
        }
        public async Task Invoke(HttpContext httpContext)
        {
            
            bool success = httpContext.Request.Headers.TryGetValue 
            (API_KEY, out var apiKeyFromHttpHeader);
            //success = true;
            //apiKeyFromHttpHeader = "813713-9154168-688634-378689";
            if (!success )
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsync("Api Key không hợp lệ!");

                return;
            }
            string? api_key_From_Configuration = Configuration[API_KEY];

            if (!string.IsNullOrEmpty(api_key_From_Configuration))
            {
                if (api_key_From_Configuration != apiKeyFromHttpHeader)
                {
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync("Api Key không chính xác: Truy cập trái phép!");

                    return;
                }
                
            }

            if (StringValues.IsNullOrEmpty(apiKeyFromHttpHeader))
            {
                httpContext.Response.StatusCode = 401;
                await httpContext.Response.WriteAsync("Api Key không chính xác: Truy cập trái phép!");
                return;
            }
           

            await _next(httpContext);
        }
    }
}
