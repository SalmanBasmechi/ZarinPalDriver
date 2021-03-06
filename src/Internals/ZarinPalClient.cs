﻿using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZarinPalDriver.Models;

namespace ZarinPalDriver.Internals
{
    internal class ZarinPalClient : IZarinPalClient
    {
        private static PaymentResponse PaymentResponse(JObject model, Mode mode)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            int code;
            string authority;

            Status status;
            Uri gatewayUri;

            if (model["data"].HasValues)
            {
                code = model["data"]["code"].Value<int>();
                authority = model["data"]["authority"].Value<string>();

                status = new Status(code);
                gatewayUri = GatewayUri.Get(mode);
            }
            else
            {
                code = model["errors"]["code"].Value<int>();
                authority = null;

                status = new Status(code);
                gatewayUri = null;
            }

            return new PaymentResponse(authority, status, gatewayUri);
        }

        private static VerificationResponse VerificationResponse(JObject model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            int code;
            string referenceId;

            if (model["data"].HasValues)
            {
                code = model["data"]["code"].Value<int>();
                referenceId = model["data"]["ref_id"].Value<string>();
            }
            else
            {
                code = model["errors"]["code"].Value<int>();
                referenceId = null;
            }

            var status = new Status(code);

            return new VerificationResponse(status, referenceId);
        }

        public PaymentResponse Send(PaymentRequest request) => SendAsync(request).Result;

        public VerificationResponse Send(VerificationRequest request) => SendAsync(request).Result;

        public async Task<PaymentResponse> SendAsync(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string baseUri = ApiBaseUri.Get(request.Mode);

            string requestUri = $"{baseUri}/request.json";
            string json = request.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();

            var response = await httpClient.PostAsync(requestUri, content, cancellationToken);

            json = await response.Content.ReadAsStringAsync();

            var model = JObject.Parse(json);

            return PaymentResponse(model, request.Mode);
        }

        public async Task<VerificationResponse> SendAsync(VerificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string baseUri = ApiBaseUri.Get(request.Mode);

            string requestUri = $"{baseUri}/verify.json";
            string json = request.ToJson();
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();

            var response = await httpClient.PostAsync(requestUri, content, cancellationToken);

            json = await response.Content.ReadAsStringAsync();

            var model = JObject.Parse(json);

            return VerificationResponse(model);
        }
    }
}
