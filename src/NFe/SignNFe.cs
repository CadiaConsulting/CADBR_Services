using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;
using AuthorizationLevel = Microsoft.Azure.Functions.Worker.AuthorizationLevel;
using HttpTriggerAttribute = Microsoft.Azure.Functions.Worker.HttpTriggerAttribute;

public class SignNFe
{
    private readonly ILogger<SignNFe> _logger;

    public SignNFe(ILogger<SignNFe> logger)
    {
        _logger = logger;
    }

    [Function("SignNFe")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

            var bodyStream = new StreamReader(req.Body);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            string XMLString = bodyStream.ReadToEnd();
            //string XMLString = await req.ReadAsStringAsync();
            string RefUri = req.Headers["RefUri"];
            string Cert = req.Headers["Cert"];
            string Pass = req.Headers["Pass"];

            string msgResultado = "";
            bool error = false;

            XmlDocument doc = new XmlDocument();

            doc.PreserveWhitespace = false;

            try
            {
                doc.LoadXml(XMLString);

                int qtdeRefUri = doc.GetElementsByTagName(RefUri).Count;

                if (qtdeRefUri == 0)
                {
                    msgResultado = "A tag de assinatura " + RefUri.Trim() + " inexiste";
                    error = true;
                }
                else if (qtdeRefUri > 1)
                {
                    msgResultado = "A tag de assinatura " + RefUri.Trim() + " não é unica";
                    error = true;
                }
                else
                    try
                    {
                        X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                        SignedXml signedXml = new SignedXml(doc); ;
                        signedXml.SigningKey = _X509Cert.PrivateKey;
                        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
                        Reference reference = new Reference();
                        XmlAttributeCollection _Uri = doc.GetElementsByTagName(RefUri).Item(0).Attributes;
                        foreach (XmlAttribute _atributo in _Uri)
                        {
                            if (_atributo.Name == "Id")
                                reference.Uri = "#" + _atributo.InnerText;
                        }

                        XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
                        reference.AddTransform(env);

                        XmlDsigC14NTransform c14 = new XmlDsigC14NTransform();
                        reference.AddTransform(c14);

                        signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                        reference.DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1";

                        signedXml.AddReference(reference);

                        KeyInfo keyInfo = new KeyInfo();

                        keyInfo.AddClause(new KeyInfoX509Data(_X509Cert));

                        signedXml.KeyInfo = keyInfo;

                        signedXml.ComputeSignature();

                        XmlElement xmlDigitalSignature = signedXml.GetXml();

                        doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

                        var XMLDoc = new XmlDocument();
                        XMLDoc.PreserveWhitespace = false;
                        XMLDoc = doc;

                        msgResultado = XMLDoc.OuterXml;
                        error = false;
                    }
                    catch (Exception caught)
                    {
                        msgResultado = "Erro: Ao assinar o documento - " + caught.Message.ToString();
                        error = true;
                    }
            }
            catch (Exception caught)
            {
                msgResultado = "Erro: XML mal formado - " + caught.Message;
                error = true;
            }

            return !error
                ? (ActionResult)new OkObjectResult(msgResultado)
                : new BadRequestObjectResult(msgResultado);
        }

    }
