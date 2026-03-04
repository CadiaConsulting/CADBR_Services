using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Xml;

public class SignNFSe_TagAssina
{
    private readonly ILogger<SignNFSe_TagAssina> _logger;

    public SignNFSe_TagAssina(ILogger<SignNFSe_TagAssina> logger)
    {
        _logger = logger;
    }

    [Function("SignNFSe_TagAssina")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        var bodyStream = new StreamReader(req.Body);
        bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
        string XMLString = bodyStream.ReadToEnd();
            
        string assinatura = req.Headers["Assinatura"];
        string Cert = req.Headers["Cert"];
        string Pass = req.Headers["Pass"];

        string msgResultado = "";
        Boolean error = false;

        try
        {
            if (Cert == "Legacy")
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = false;
                doc.LoadXml(XMLString);
                XmlNode CertNode = doc.GetElementsByTagName("b64").Item(0);
                Cert = CertNode.InnerText;
                CertNode.ParentNode.RemoveChild(CertNode);
            }

            X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.Exportable);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] sAssinaturaByte = enc.GetBytes(assinatura);

            RSA rsa = _X509Cert.GetRSAPrivateKey();
            //RSACryptoServiceProvider rsa = _X509Cert.PrivateKey as RSACryptoServiceProvider;
            RSAPKCS1SignatureFormatter rsaf = new RSAPKCS1SignatureFormatter(rsa);
            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

            byte[] hash;
            hash = sha1.ComputeHash(sAssinaturaByte);

            rsaf.SetHashAlgorithm("SHA1");
            sAssinaturaByte = rsaf.CreateSignature(hash);

            msgResultado = Convert.ToBase64String(sAssinaturaByte);
            error = false;

        }
        catch (Exception caught)
        {
            msgResultado = "Erro: Ao assinar - " + caught.Message.ToString();
            error = true;
        }

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);
    }

}
