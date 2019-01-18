// Copyright (c) 2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace nhitomi
{
    // Thanks Stephen Cleary
    // http://blog.stephencleary.com/2016/11/streaming-zip-on-aspnet-core.html
    public class FileCallbackResult : FileResult
    {
        private Func<Stream, ActionContext, Task> _callback;

        public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback) : base(contentType?.ToString())
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            _callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
            {
                SetHeadersAndLog(context, result, null, result.EnableRangeProcessing, result.LastModified, result.EntityTag);
                return result._callback(context.HttpContext.Response.Body, context);
            }
        }
    }
}