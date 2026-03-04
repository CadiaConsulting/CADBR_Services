using Azure;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO.Compression;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Xml;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SendNFe
{
    private readonly ILogger<SendNFe> _logger;

    public SendNFe(ILogger<SendNFe> logger)
    {
        _logger = logger;
    }

    [Function("SendNFe")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        string msgResultado = "";
        bool error = false;

        try
        {

            var bodyStream = new StreamReader(req.Body);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            string XMLString = bodyStream.ReadToEnd();
            string Action = req.Headers["Action"];
            string Cert = req.Headers["Cert"];
            string Pass = req.Headers["Pass"];
            string Url = req.Headers["URL"];
            string UF = req.Headers["UF"];

            X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);
            XmlDocument doc = new XmlDocument();

            doc.PreserveWhitespace = false;
            doc.LoadXml(XMLString);

            System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
            System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
            textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap12, System.ServiceModel.Channels.AddressingVersion.None);
            bnd.Elements.Add(textBindingElement);
            System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
            httpsBindingElement.AllowCookies = true;
            httpsBindingElement.MaxBufferSize = int.MaxValue;
            httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
            httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
            httpsBindingElement.RequireClientCertificate = true;
            httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
            bnd.Elements.Add(httpsBindingElement);

            switch (Action.ToUpper())
            {
                case "NFERECEPCAO":
                    {
                        msgResultado = NfeRecepcao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }
                
                case "NFERETRECEPCAO":
                    {
                        msgResultado = NfeRetRecepcao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFECANCELAMENTO":
                    {
                        msgResultado = NfeCancelamento(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFEINUTILIZACAO":
                    {
                        msgResultado = NfeInutilizacao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFECONSULTAPROTOCOLO":
                    {
                        msgResultado = NfeConsultaProtocolo(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFESTATUSSERVICO":
                    {
                        msgResultado = NfeStatusServico(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "CADCONSULTACADASTRO":
                    {
                        msgResultado = CadConsultaCadastro(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "CARTACORRECAO":
                    {
                        msgResultado = NfeCartaCorrecao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFEAUTORIZACAO":
                    {
                        msgResultado = NfeAutorizacao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                case "NFERETAUTORIZACAO":
                    {
                        msgResultado = NfeRetAutorizacao(doc, Url, bnd ,Cert, Pass, UF);
                        break;
                    }

                default:
                    {
                        msgResultado = "Not implemented yet: " + Action;
                        break;
                    }
                
            }
        }
        catch (Exception caught)
        {
            msgResultado = "Erro: " + caught.Message;
            error = true;
        }

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);

    }

    public static string NfeRecepcao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        string envXml = "<enviNFe versao=\"4.00\" xmlns =\"http://www.portalfiscal.inf.br/nfe\" ><idLote>1</idLote><indSinc>1</indSinc>" + xmlDados.OuterXml + "</enviNFe>";
        //string envXml = "<enviNFe versao=\"{0}\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><idLote>{1}</idLote>{2}</enviNFe>";
        //envXml = string.Format(envXml, "4.00", "1", xmlDados.OuterXml);
        //envXml= envXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(envXml);

        ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient ws_client = new ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest ws_request = new ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest(xmlDoc);
        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoResponse ws_response = ws_client.nfeRecepcaoEvento(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeRetRecepcao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        string envXml = "<enviNFe versao=\"{0}\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><idLote>{1}</idLote>{2}</enviNFe>";
        envXml = string.Format(envXml, "4.00", "1", xmlDados.OuterXml);
        //envXml= envXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(envXml);

        ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient ws_client = new ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest ws_request = new ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest(xmlDoc);
        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoResponse ws_response = ws_client.nfeRecepcaoEvento(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeCancelamento(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        string envXml = "<envEvento versao=\"{0}\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><idLote>{1}</idLote>{2}</envEvento>";
        envXml = string.Format(envXml, "1.00", "1", xmlDados.OuterXml);
        //envXml= envXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(envXml);

        ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient ws_client = new ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest ws_request = new ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest(xmlDoc);
        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoResponse ws_response = ws_client.nfeRecepcaoEvento(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeInutilizacao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        ws_NFeInutilizacao4.NFeInutilizacao4SoapClient ws_client = new ws_NFeInutilizacao4.NFeInutilizacao4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeInutilizacao4.nfeInutilizacaoNFRequest ws_request = new ws_NFeInutilizacao4.nfeInutilizacaoNFRequest(xmlDados);
        ws_NFeInutilizacao4.nfeInutilizacaoNFResponse ws_response = ws_client.nfeInutilizacaoNF(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeConsultaProtocolo(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        ws_NFeConsultaProtocolo4.NFeConsultaProtocolo4SoapClient ws_client = new ws_NFeConsultaProtocolo4.NFeConsultaProtocolo4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeConsultaProtocolo4.nfeConsultaNFRequest ws_request = new ws_NFeConsultaProtocolo4.nfeConsultaNFRequest(xmlDados);
        ws_NFeConsultaProtocolo4.nfeConsultaNFResponse ws_response = ws_client.nfeConsultaNF(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeStatusServico(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        ws_NFeStatusServico4.NFeStatusServico4SoapClient ws_client = new ws_NFeStatusServico4.NFeStatusServico4SoapClient(bnd, new EndpointAddress(Url));

        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeStatusServico4.nfeStatusServicoNFRequest ws_request = new ws_NFeStatusServico4.nfeStatusServicoNFRequest(xmlDados);
        ws_NFeStatusServico4.nfeStatusServicoNFResponse ws_response = ws_client.nfeStatusServicoNF(ws_request);

        return ws_response.nfeResultMsg.OuterXml.ToString();
    }

    public static string CadConsultaCadastro(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        ws_CadConsultaCadastro4.CadConsultaCadastro4Soap12Client ws_client = new ws_CadConsultaCadastro4.CadConsultaCadastro4Soap12Client(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_CadConsultaCadastro4.consultaCadastroRequest ws_request = new ws_CadConsultaCadastro4.consultaCadastroRequest(xmlDados);
        ws_CadConsultaCadastro4.consultaCadastroResponse ws_response = ws_client.consultaCadastro(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeCartaCorrecao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        string envXml = "<envEvento versao=\"{0}\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><idLote>{1}</idLote>{2}</envEvento>";
        envXml = string.Format(envXml, "1.00", "1", xmlDados.OuterXml);
        //envXml= envXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(envXml);

        ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient ws_client = new ws_NFeRecepcaoEvento4.NFeRecepcaoEvento4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest ws_request = new ws_NFeRecepcaoEvento4.nfeRecepcaoEventoRequest(xmlDoc);
        ws_NFeRecepcaoEvento4.nfeRecepcaoEventoResponse ws_response = ws_client.nfeRecepcaoEvento(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeAutorizacao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        string envXml = "<enviNFe versao=\"4.00\" xmlns =\"http://www.portalfiscal.inf.br/nfe\" ><idLote>1</idLote><indSinc>0</indSinc>" + xmlDados.OuterXml + "</enviNFe>";
        //envXml= envXml.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", "");

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(envXml);

        ws_NFeAutorizacao4.NFeAutorizacao4SoapClient ws_client = new ws_NFeAutorizacao4.NFeAutorizacao4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeAutorizacao4.nfeAutorizacaoLoteRequest ws_request = new ws_NFeAutorizacao4.nfeAutorizacaoLoteRequest(xmlDoc);
        ws_NFeAutorizacao4.nfeAutorizacaoLoteResponse ws_response = ws_client.nfeAutorizacaoLote(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }

    public static string NfeRetAutorizacao(XmlDocument mainXmldoc, string Url, CustomBinding bnd, string Cert, string Pass, string UF)
    {
        XmlNode xmlDados;
        xmlDados = mainXmldoc.DocumentElement;

        ws_NFeRetAutorizacao4.NFeRetAutorizacao4SoapClient ws_client = new ws_NFeRetAutorizacao4.NFeRetAutorizacao4SoapClient(bnd, new EndpointAddress(Url));
        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        ws_NFeRetAutorizacao4.nfeRetAutorizacaoLoteRequest ws_request = new ws_NFeRetAutorizacao4.nfeRetAutorizacaoLoteRequest(xmlDados);
        ws_NFeRetAutorizacao4.nfeRetAutorizacaoLoteResponse ws_response = ws_client.nfeRetAutorizacaoLote(ws_request);

        return ws_response.nfeResultMsg.OuterXml;
    }
    
}

