using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AddonProject.Handlers
{
    public class AddonApiVersionRedirectHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsolutePath.StartsWith("/api/Memo/MemoDetail/MAdvancveFormByMemoIds"))
            {
                // สร้าง Request ใหม่โดยใช้ URI ใหม่
                var newRequest = new HttpRequestMessage(request.Method, new Uri(request.RequestUri.AbsoluteUri.Replace("/api/Memo/MemoDetail/MAdvancveFormByMemoIds", "/api/v1/addonThaiLife/MAdvancveFormByMemoIds")))
                {
                    Content = request.Content,
                    Version = request.Version
                };

                // คัดลอก Headers จาก Request เดิมไปยัง Request ใหม่
                foreach (var header in request.Headers)
                {
                    newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // ส่ง Request ใหม่ไปยัง Pipeline
                return await base.SendAsync(newRequest, cancellationToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
