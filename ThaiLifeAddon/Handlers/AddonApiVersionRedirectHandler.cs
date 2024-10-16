﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ThaiLifeAddon.Helpers;

namespace AddonProject.Handlers
{
    public class AddonApiVersionRedirectHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsolutePath.Contains("/api/Memo/MemoDetail/MAdvancveFormByMemoIds"))
            {
                WriteLogFile.LogAddon("Request Condition Redirect Result : " + request.RequestUri.AbsolutePath.Contains("/api/Memo/MemoDetail/MAdvancveFormByMemoIds"));
                WriteLogFile.LogAddon($"ChangePath To /api/v1/addonThaiLife/MAdvancveFormByMemoIds");
                // สร้าง Request ใหม่โดยใช้ URI 
                var newRequest = CloneAndModifyHttpRequest(request, "/api/Memo/MemoDetail/MAdvancveFormByMemoIds", "/api/v1/addonThaiLife/MAdvancveFormByMemoIds");
                // ส่ง Request ใหม่ไปยัง Pipeline
                return await base.SendAsync(newRequest, cancellationToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        public HttpRequestMessage CloneAndModifyHttpRequest(HttpRequestMessage request, string oldPath, string newPath)
        {
            var newRequest = new HttpRequestMessage(request.Method,
                new Uri(request.RequestUri.AbsoluteUri
                .Replace(oldPath, newPath)))
            {
                Content = request.Content,
                Version = request.Version
            };

            // คัดลอก Headers จาก Request เดิมไปยัง Request ใหม่
            foreach (var header in request.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newRequest;
        }
    }
}
