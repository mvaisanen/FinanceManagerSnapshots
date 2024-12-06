using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(context, exception);
            }
        }

        private static Task HandleErrorAsync(HttpContext context, Exception exception)
        {
            if (exception is AkkaError)
            {
                var exp = (AkkaError)exception;
                var cont = JsonConvert.SerializeObject(new { message = exp.Message });
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = exp.StatusCode;
                return context.Response.WriteAsync(cont);
            }
            else if (exception is UnauthorizedError)
            {
                var cont = JsonConvert.SerializeObject(new { message = exception.Message });
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsync(cont);
            }


            var response = new { message = exception.Message };
            var payload = JsonConvert.SerializeObject(response);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400;

            return context.Response.WriteAsync(payload);
        }
    }

    public class AkkaError : ApiError
    {
        public AkkaError(int statusCode, string msgToUser) : base(statusCode, msgToUser)
        {
        }
    }
    public class UnauthorizedError : ApiError
    {
        public UnauthorizedError(string msgToUser) : base(StatusCodes.Status401Unauthorized, msgToUser)
        {
        }
    }

    abstract public class ApiError: Exception
    {
        public int StatusCode { get; set; }

        public ApiError(int statusCode, string msgToUser): base(msgToUser)
        {
            StatusCode = statusCode;
        }
    }

}
