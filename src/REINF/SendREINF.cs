using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;

public class SendREINF
{
    private readonly ILogger<SendREINF> _logger;

    public SendREINF(ILogger<SendREINF> logger)
    {
        _logger = logger;
    }

    [Function("SendREINF")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        var bodyStream = new StreamReader(req.Body);
        bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
        string XMLString = bodyStream.ReadToEnd();

        string Cert = req.Headers["Cert"];
        string Pass = req.Headers["Pass"];
        string Action = req.Headers["Action"].ToString().ToUpper();
        string URL = req.Headers["URL"];

        string tipoInscricao = req.Headers["tipoInscricao"];
        string numeroInscricao = req.Headers["numeroInscricao"];
        string numeroRecibo = req.Headers["numeroRecibo"];

        string msgResultado = "";
        Boolean error = false;
        XmlDocument doc = new XmlDocument();

        doc.PreserveWhitespace = false;

        try
        {

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

            doc.LoadXml(XMLString);

            if (Action == "RECEPCAOLOTE")
            {                                       

                XmlElement xmlDados = doc.DocumentElement;
                ws_recepcaolote.RecepcaoLoteReinfClient ws_client = new ws_recepcaolote.RecepcaoLoteReinfClient(bnd, new EndpointAddress(URL));
                ws_recepcaolote.ReceberLoteEventosRequest ws_request = new ws_recepcaolote.ReceberLoteEventosRequest(new ws_recepcaolote.ReceberLoteEventosRequestBody(xmlDados));
                ws_client.ReceberLoteEventos(ws_request);

            }
            else
            if (Action == "CONSULTA")
            {

                XmlElement xmlDados = doc.DocumentElement;
                ws_consultas.ConsultasReinfClient ws_client = new ws_consultas.ConsultasReinfClient(bnd, new EndpointAddress(URL));
                ws_consultas.ConsultaResultadoFechamento2099Request ws_request = new ws_consultas.ConsultaResultadoFechamento2099Request(new ws_consultas.ConsultaResultadoFechamento2099RequestBody(Convert.ToByte(tipoInscricao), numeroInscricao, numeroRecibo));
                ws_client.ConsultaResultadoFechamento2099(ws_request);

            }
            else
                msgResultado = "Erro: Ação " + Action + " não implementada";
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

