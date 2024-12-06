using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using System.Threading.Tasks;

namespace Server
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        Logger log;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
            log = LogManager.GetCurrentClassLogger();
        }

        public async Task Invoke(HttpContext context)
        {
            //First, get the incoming request
            var request = await FormatRequest(context.Request);

            log.Debug(request);

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                //...and use that for the temporary response body
                context.Response.Body = responseBody;

                //Continue down the Middleware pipeline, eventually returning to this class
                await _next(context);

                //Format the response from the server
                var response = await FormatResponse(context.Response);

                //TODO: Save log to chosen datastore
                log.Debug(response);

                if (context.Response.StatusCode != 204) // 204 -> No body, copying would result to exception
                    //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                    await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            //This line allows us to set the reader for the request back at the beginning of its stream.
            //request.EnableRewind();
            request.EnableBuffering();

            var body = request.Body; //moved this after the enable rewind...

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer, 0, buffer.Length);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            //..and finally, assign the read body back to the request body, which is allowed because of EnableRewind()
            body.Seek(0, SeekOrigin.Begin); //added to make this work, maybe...
            request.Body = body;
            string requestTarget = $"{request.Scheme}://{request.Host}{request.Path} {request.QueryString}";
            StringBuilder sb = new StringBuilder();
            var method = request.Method;
            sb.AppendLine($"[Request] {method} {requestTarget}");
            sb.AppendLine("Headers:");
            foreach (var header in request.Headers.ToList())
            {
                sb.AppendLine(header.Key + "=" + header.Value);
            }
            sb.AppendLine("Request body:");
            sb.AppendLine(bodyAsText);

            return sb.ToString();
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Response] Status code: " + response.StatusCode);
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers.ToList())
            {
                sb.AppendLine(header.Key + "=" + header.Value);
            }
            sb.AppendLine("Content:");
            sb.AppendLine(text);
            return sb.ToString();

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            //return $"{response.StatusCode}: {text}";

        }
    }

}
