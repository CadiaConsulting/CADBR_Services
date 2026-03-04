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
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SignNFSeNacional
{
    private readonly ILogger<SignNFSeNacional> _logger;

    public SignNFSeNacional(ILogger<SignNFSeNacional> logger)
    {
        _logger = logger;
    }

    [Function("SignNFSeNacional")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {
        string msgResultado = "";
        bool error = false;
        try 
        {
            string body;
            using (var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                body = await reader.ReadToEndAsync();

            string Cert = req.Headers["Cert"].ToString();
            string Pass = req.Headers["Pass"].ToString();

            string RefUri = req.Headers["RefUri"].ToString();


            if (Cert == "Legacy")
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(body);
                XmlNode CertNode = doc.GetElementsByTagName("b64").Item(0);
                Cert = CertNode.InnerText;
                CertNode.ParentNode.RemoveChild(CertNode);
                body = doc.OuterXml;
            }

            X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.Exportable);

            msgResultado = SignSHA1(body, _X509Cert, RefUri);
        }

        catch (Exception caught)
        {
            var rootCause = caught.GetBaseException();
            msgResultado = "Erro: "+ rootCause.Message;
            error = true;
        }

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);
    }

    public static string SignSHA1(string xml, X509Certificate2 certificate, string referenceTagName)
    {

        var document = new XmlDocument();
        document.LoadXml(xml);

        XmlNode signTag = document.DocumentElement;

        XmlNode referenceTag = document.GetElementsByTagName(referenceTagName)[0];

        var referenceUri = referenceTag is not null ? '#' + (referenceTag.Attributes["Id"] ?? referenceTag.Attributes["id"]).Value : string.Empty;

        var reference = new Reference(uri: referenceUri)
        {
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());

        var signedXml = new SignedXml(signTag as XmlElement);
        signedXml.SigningKey = certificate.GetRSAPrivateKey();
        signedXml.KeyInfo = new KeyInfo();
        signedXml.KeyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        signedXml.AddReference(reference);
        signedXml.ComputeSignature();

        signTag.AppendChild(document.ImportNode(signedXml.GetXml(), true));
        return document.OuterXml;
    }

}