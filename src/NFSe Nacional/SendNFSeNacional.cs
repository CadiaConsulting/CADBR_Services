using Azure;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO.Compression;
using System.Reflection.PortableExecutable;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Xml;

public class SendNFSeNacional
{
    private readonly ILogger<SendNFSeNacional> _logger;

    public SendNFSeNacional(ILogger<SendNFSeNacional> logger)
    {
        _logger = logger;
    }

    [Function("SendNFSeNacional")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        string msgResultado = "";
        bool error = false;
        string[] Actions = { "RECEPCIONARLOTERPS", "CONSULTA", "CANCELAR" };
        
        string statuscode = "000";
        var xmlInd = @"<?xml version=""1.0"" encoding=""utf-8""?>";

        try
        {

            string body;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                body = await reader.ReadToEndAsync();

            string Amb = req.Headers["Amb"].ToString().ToUpper();
            string Action = req.Headers["Action"].ToString().ToUpper();
            string Cert = req.Headers["Cert"].ToString();
            string Pass = req.Headers["Pass"].ToString();

            string URL = "";
            if (Amb == "HOM")
                URL = @"https://sefin.producaorestrita.nfse.gov.br/sefinnacional/";
            else
                URL = @"https://sefin.nfse.gov.br/sefinnacional/";

            if (Cert == "Legacy")
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(body);
                XmlNode CertNode = doc.GetElementsByTagName("b64").Item(0);
                Cert = CertNode.InnerText;
                CertNode.ParentNode.RemoveChild(CertNode);
                string tmpbody = doc.OuterXml;
                string RefUri = req.Headers["RefUri"].ToString();

                X509Certificate2 _X509CertTemp = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.Exportable);
                body = SignNFSeNacional.SignSHA1(tmpbody, _X509CertTemp, RefUri);

            }

            X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.Exportable);

            if (Action == "RECEPCIONARLOTERPS") 
            {
                // leitura do xml
                //var signedXml = SignSHA1(body, _X509Cert, "DPS", "infDPS");
                var signedXml = body;

                var gziped = new MemoryStream();
                using (var gzip = new GZipStream(gziped, CompressionMode.Compress))
                    gzip.Write(Encoding.UTF8.GetBytes(signedXml));
                var dpsXmlGZipB64 = Convert.ToBase64String(gziped.ToArray());

                // gravação do json
                var json = $"{{\"dpsXmlGZipB64\":\"{dpsXmlGZipB64}\"}}";

                // emissão da nfse
                var response = CreateHttpClient(_X509Cert, URL).PostAsync("nfse",
                    new StringContent(json, Encoding.UTF8, "application/json"))
                    .GetAwaiter().GetResult();

                string jsonresptxt = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                statuscode = response.StatusCode.ToString();

                // tratamento da resposta da emissão
                //var defNFSeReponse = new { idDps = string.Empty, chaveAcesso = string.Empty, nfseXmlGZipB64 = string.Empty, erros = new[] { new { Parametros = string.Empty, Codigo = string.Empty, Descricao = string.Empty, Complemento = string.Empty } } };
                //var jsonResponse = JsonConvert.DeserializeAnonymousType(jsonresptxt, defNFSeReponse);

                var xmlNode = JsonConvert.DeserializeXmlNode(jsonresptxt, "Root");
                msgResultado = xmlNode != null ? xmlInd + xmlNode.OuterXml : string.Empty;

            }
            if (Action == "CONSULTA")
            {
                // consulta da nfse
                string chaveAcesso = req.Headers["Cab"].ToString();
                var response = CreateHttpClient(_X509Cert, URL).GetAsync($"nfse/{chaveAcesso}")
                .GetAwaiter().GetResult();

                //var defGetNFSeReponse = new { chaveAcesso = string.Empty, nfseXmlGZipB64 = string.Empty, erro = new { Parametros = string.Empty, Codigo = string.Empty, Descricao = string.Empty, Complemento = string.Empty } };
                //var jsonResponse = JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), defGetNFSeReponse);

                string jsonresptxt = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                statuscode = response.StatusCode.ToString();

                var xmlNode = JsonConvert.DeserializeXmlNode(jsonresptxt, "Root");
                msgResultado = xmlNode != null ? xmlInd + xmlNode.OuterXml : string.Empty;

                //// Parse do JSON
                //using var doc = JsonDocument.Parse(jsonresptxt);
                //string? base64Data = doc.RootElement.GetProperty("nfseXmlGZipB64").GetString();

                //if (string.IsNullOrWhiteSpace(base64Data))
                //{
                //    msgResultado = "Erro: Status " + statuscode + " - " + "nfseXmlGZipB64 não encontrado na resposta.";
                //    error = true;
                //}
                //else
                //{
                //    // Decodifica Base64
                //    byte[] gzippedData = Convert.FromBase64String(base64Data);

                //    // Descompacta GZip
                //    using var compressedStream = new MemoryStream(gzippedData);
                //    using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                //    using var reader = new StreamReader(gzipStream, Encoding.UTF8);
                //    msgResultado = reader.ReadToEnd();
                //}
                
            }
            if (Action == "CANCELAR")
            {
                // leitura do xml de cancelamento
                string chaveAcesso = req.Headers["Cab"].ToString();
                //var signedXml = SignSHA1(body, _X509Cert, "pedRegEvento", "infPedReg");
                var signedXml = body;

                var gziped = new MemoryStream();
                using (var gzip = new GZipStream(gziped, CompressionMode.Compress))
                    gzip.Write(Encoding.UTF8.GetBytes(signedXml));
                var dpsXmlGZipB64 = Convert.ToBase64String(gziped.ToArray());

                // gravação do json
                var json = $"{{\"pedidoRegistroEventoXmlGZipB64\":\"{dpsXmlGZipB64}\"}}";

                // emissão de cancelamento
                var response = CreateHttpClient(_X509Cert, URL).PostAsync($"nfse/{chaveAcesso}/eventos",
                    new StringContent(json, Encoding.UTF8, "application/json"))
                    .GetAwaiter().GetResult();

                string jsonresptxt = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                statuscode = response.StatusCode.ToString();

                var xmlNode = JsonConvert.DeserializeXmlNode(jsonresptxt, "Root");
                msgResultado = xmlNode != null ? xmlInd + xmlNode.OuterXml : string.Empty;

            }
        }
        catch (Exception caught)
        {
            var rootCause = caught.GetBaseException();
            msgResultado = "Erro: Status " + statuscode + " - " + rootCause.Message;
            error = true;
        }

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);
    }


    public static HttpClient CreateHttpClient(X509Certificate2 _X509Cert, string url)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = SslProtocols.Tls12
        };
        handler.ClientCertificates.Add(_X509Cert);

        return new HttpClient(handler)
        {
            BaseAddress = new System.Uri(url) 
        };
    }




}