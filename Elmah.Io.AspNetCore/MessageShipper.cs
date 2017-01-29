﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elmah.Io.AspNetCore.Extensions;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Microsoft.AspNetCore.Http;

namespace Elmah.Io.AspNetCore
{
    internal class MessageShipper
    {
        public static async Task ShipAsync(string apiKey, Guid logId, string title, HttpContext context,
            ElmahIoSettings settings)
        {
            await ShipAsync(apiKey, logId, title, context, settings, null);
        }

        public static void Ship(string apiKey, Guid logId, string title, HttpContext context, ElmahIoSettings settings)
        {
            Ship(apiKey, logId, title, context, settings, null);
        }

        public static async Task ShipAsync(string apiKey, Guid logId, string title, HttpContext context,
            ElmahIoSettings settings, Exception exception)
        {
            apiKey.AssertApiKey();
            logId.AssertLogId();
            settings.AssertSettings();

            var elmahioApi = ElmahioAPI.Create(apiKey);
            var createMessage = new CreateMessage
            {
                DateTime = DateTime.UtcNow,
                Detail = Detail(exception, settings),
                Type = exception?.GetType().Name,
                Title = title,
                Data = exception?.ToDataList(),
                Cookies = Cookies(context),
                Form = Form(context),
                Hostname = context.Request?.Host.Host,
                ServerVariables = ServerVariables(context),
                StatusCode = context.Response?.StatusCode,
                Url = context.Request?.Path.Value,
                QueryString = QueryString(context),
                Method = context.Request?.Method,
            };

            elmahioApi.Messages.OnMessage += (sender, args) => {
                settings.OnMessage?.Invoke(args.Message);
            };
            elmahioApi.Messages.OnMessageFail += (sender, args) =>
            {
                settings.OnError?.Invoke(args.Message, args.Error);
            };

            try
            {
                await elmahioApi.Messages.CreateAndNotifyAsync(logId, createMessage);
            }
            catch (Exception e)
            {
                settings.OnError?.Invoke(createMessage, e);
                // If there's a Exception while generating the error page, re-throw the original exception.
            }
        }

        public static void Ship(string apiKey, Guid logId, string title, HttpContext context, ElmahIoSettings settings, Exception exception)
        {
            Task.Factory.StartNew(s => ShipAsync(apiKey, logId, title, context, settings, exception), null, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap().GetAwaiter().GetResult();
        }

        private static string Detail(Exception exception, ElmahIoSettings settings)
        {
            if (exception == null) return null;
            return settings.ExceptionFormatter != null
                ? settings.ExceptionFormatter.Format(exception)
                : exception.ToString();
        }

        private static List<Item> Cookies(HttpContext httpContext)
        {
            return httpContext.Request?.Cookies?.Keys.Select(k => new Item(k, httpContext.Request.Cookies[k])).ToList();
        }

        private static List<Item> Form(HttpContext httpContext)
        {
            try
            {
                return httpContext.Request?.Form?.Keys.Select(k => new Item(k, httpContext.Request.Form[k])).ToList();
            }
            catch (InvalidOperationException)
            {
                // Request not a form POST or similar
            }

            return null;
        }

        private static List<Item> ServerVariables(HttpContext httpContext)
        {
            return httpContext.Request?.Headers?.Keys.Select(k => new Item(k, httpContext.Request.Headers[k])).ToList();
        }

        private static List<Item> QueryString(HttpContext httpContext)
        {
            return httpContext.Request?.Query?.Keys.Select(k => new Item(k, httpContext.Request.Query[k])).ToList();
        }
    }
}