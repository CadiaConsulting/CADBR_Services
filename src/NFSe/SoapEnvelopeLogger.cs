//using System;
//using System.IO;
//using System.ServiceModel;
//using System.ServiceModel.Channels;
//using System.ServiceModel.Description;
//using System.ServiceModel.Dispatcher;
//using System.Text;
//using System.Xml;

///// <summary>
///// Captura o envelope SOAP exatamente como ele vai para a rede e como volta.
///// Grava em disco e mantem a ultima troca em memoria.
/////
///// Uso:
/////     var soapLog = SoapEnvelopeLogger.AttachTo(ws_client, "saocaetano");
/////     ... chamada do web service ...
/////     soapLog.Describe()
/////
///// Saida: variavel de ambiente SOAP_LOG_DIR, ou %TEMP%\CADBRServices\soap.
///// </summary>
//public class SoapEnvelopeLogger : IClientMessageInspector, IEndpointBehavior
//{
//    public string RequestEnvelope { get; private set; } = "";
//    public string ResponseEnvelope { get; private set; } = "";

//    /// <summary>Conteudo de nfseDadosMsg ja desescapado: e exatamente o XML que o municipio valida contra o XSD.</summary>
//    public string RequestPayload { get; private set; } = "";

//    /// <summary>Conteudo de outputXML ja desescapado.</summary>
//    public string ResponsePayload { get; private set; } = "";

//    public string LogDirectory { get; }

//    private readonly string _tag;
//    private readonly string _stamp;
//    private readonly string _requestPayloadTag;
//    private readonly string _responsePayloadTag;

//    public SoapEnvelopeLogger(string tag, string requestPayloadTag = "nfseDadosMsg", string responsePayloadTag = "outputXML")
//    {
//        _tag = tag;
//        _requestPayloadTag = requestPayloadTag;
//        _responsePayloadTag = responsePayloadTag;
//        _stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");

//        LogDirectory = Environment.GetEnvironmentVariable("SOAP_LOG_DIR")
//            ?? Path.Combine(Path.GetTempPath(), "CADBRServices", "soap");
//    }

//    /// <summary>Liga o logger no endpoint. Precisa ser chamado antes da primeira chamada ao servico.</summary>
//    public static SoapEnvelopeLogger AttachTo<TChannel>(ClientBase<TChannel> client, string tag,
//        string requestPayloadTag = "nfseDadosMsg", string responsePayloadTag = "outputXML")
//        where TChannel : class
//    {
//        SoapEnvelopeLogger logger = new SoapEnvelopeLogger(tag, requestPayloadTag, responsePayloadTag);
//        client.Endpoint.EndpointBehaviors.Add(logger);
//        return logger;
//    }

//    public object BeforeSendRequest(ref Message request, IClientChannel channel)
//    {
//        RequestEnvelope = Capture(ref request);
//        RequestPayload = ExtractInnerText(RequestEnvelope, _requestPayloadTag);

//        Save("request-envelope.xml", RequestEnvelope);
//        Save("request-" + _requestPayloadTag + ".xml", RequestPayload);

//        return null!;
//    }

//    public void AfterReceiveReply(ref Message reply, object correlationState)
//    {
//        ResponseEnvelope = Capture(ref reply);
//        ResponsePayload = ExtractInnerText(ResponseEnvelope, _responsePayloadTag);

//        Save("response-envelope.xml", ResponseEnvelope);
//        Save("response-" + _responsePayloadTag + ".xml", ResponsePayload);
//    }

//    /// <summary>Dump legivel da troca completa, para devolver no corpo da resposta HTTP.</summary>
//    public string Describe()
//    {
//        StringBuilder sb = new StringBuilder();

//        sb.AppendLine("===== SOAP LOG =====");
//        sb.AppendLine("Arquivos: " + LogDirectory);
//        sb.AppendLine();
//        sb.AppendLine("----- REQUEST ENVELOPE -----");
//        sb.AppendLine(RequestEnvelope);
//        sb.AppendLine();
//        sb.AppendLine("----- " + _requestPayloadTag + " (desescapado: o que o municipio valida contra o XSD) -----");
//        sb.AppendLine(RequestPayload);
//        sb.AppendLine();
//        sb.AppendLine("----- RESPONSE ENVELOPE -----");
//        sb.AppendLine(ResponseEnvelope);
//        sb.AppendLine("===== FIM SOAP LOG =====");
//        sb.AppendLine();

//        return sb.ToString();
//    }

//    /// <summary>
//    /// Serializa a mensagem sem consumi-la: WriteMessage so pode ser chamado uma vez,
//    /// entao bufferiza, devolve uma copia intacta ao pipeline e serializa outra.
//    /// </summary>
//    private static string Capture(ref Message message)
//    {
//        MessageBuffer buffer = message.CreateBufferedCopy(int.MaxValue);
//        message = buffer.CreateMessage();

//        StringBuilder sb = new StringBuilder();
//        XmlWriterSettings settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };

//        using (Message copy = buffer.CreateMessage())
//        using (XmlWriter writer = XmlWriter.Create(sb, settings))
//        {
//            copy.WriteMessage(writer);
//        }

//        return sb.ToString();
//    }

//    private static string ExtractInnerText(string envelope, string localName)
//    {
//        try
//        {
//            XmlDocument doc = new XmlDocument();
//            doc.PreserveWhitespace = true;
//            doc.LoadXml(envelope);

//            XmlNode? node = doc.SelectSingleNode("//*[local-name()='" + localName + "']");
//            return node != null ? node.InnerText : "";
//        }
//        catch (Exception)
//        {
//            return "";
//        }
//    }

//    private void Save(string suffix, string content)
//    {
//        if (string.IsNullOrEmpty(content))
//            return;

//        try
//        {
//            Directory.CreateDirectory(LogDirectory);
//            File.WriteAllText(Path.Combine(LogDirectory, _stamp + "-" + _tag + "-" + suffix), content, new UTF8Encoding(false));
//        }
//        catch (Exception)
//        {
//            //logar nunca pode derrubar o envio
//        }
//    }

//    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
//    {
//    }

//    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
//    {
//        clientRuntime.ClientMessageInspectors.Add(this);
//    }

//    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
//    {
//    }

//    public void Validate(ServiceEndpoint endpoint)
//    {
//    }
//}
