using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

public class SendNFSe
{
    private readonly ILogger<SendNFSe> _logger;

    public SendNFSe(ILogger<SendNFSe> logger)
    {
        _logger = logger;
    }

    [Function("SendNFSe")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        string msgResultado = "";
        Boolean error = false;
        string[] Cities = { "RIO DE JANEIRO", "SAO PAULO", "FORTALEZA", "BARUERI", "MACAIBA", "BELO HORIZONTE", "OSASCO", "SAOCAETANO" };
        string[] Actions = { "CONSULTA", "RECEPCIONARLOTERPS" };

        //SoapEnvelopeLogger? soapLog = null;
        //Boolean debug = req.Headers["Debug"].ToString().ToUpper() == "TRUE";

        try
        {

            string XMLString = "";
            using (var bodyStream = new StreamReader(
                req.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true))
            {
                XMLString = await bodyStream.ReadToEndAsync();
            }

            string City = req.Headers["City"].ToString().ToUpper();
            string Amb = req.Headers["Amb"].ToString().ToUpper();
            string Action = req.Headers["Action"].ToString().ToUpper();
            string Cert = req.Headers["Cert"];
            string Pass = req.Headers["Pass"];
            string Cab = req.Headers["Cab"];
            string URL = "";
            string TaxReform = req.Headers["TaxReform"].ToString().ToUpper();

            XmlDocument doc = new XmlDocument();
                
            doc.PreserveWhitespace = false;
            doc.LoadXml(XMLString);

            if (Cert == "Legacy")
            {
                try 
                {
                    XmlNode CertNode = doc.GetElementsByTagName("b64").Item(0);
                    Cert = CertNode.InnerText;
                    CertNode.ParentNode.RemoveChild(CertNode);
                }
                catch (Exception caught)
                {
                    //Do Nothing
                }
            }

            if ((Cities.Contains(City)) & (Actions.Contains(Action))) 
            {

                if (City == "BARUERI")
                {
                    System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
                    System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                    textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap11, System.ServiceModel.Channels.AddressingVersion.None);
                    bnd.Elements.Add(textBindingElement);
                    System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                    httpsBindingElement.AllowCookies = true;
                    httpsBindingElement.MaxBufferSize = int.MaxValue;
                    httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                    httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
                    httpsBindingElement.RequireClientCertificate = true;
                    httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    bnd.Elements.Add(httpsBindingElement);

                    ws_barueri.wsRPSSoapClient ws_client = new ws_barueri.wsRPSSoapClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        ws_barueri.NFeLoteEnviarArquivoRequest ws_request = new ws_barueri.NFeLoteEnviarArquivoRequest(1, XMLString);
                        ws_barueri.NFeLoteEnviarArquivoResponse ws_response = ws_client.NFeLoteEnviarArquivo(ws_request);
                        msgResultado = ws_response.NFeLoteEnviarArquivoResult.ListaMensagemRetorno.Mensagem.ToString();
                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_barueri.NFeLoteStatusArquivoRequest ws_request = new ws_barueri.NFeLoteStatusArquivoRequest(1, XMLString);
                        ws_barueri.NFeLoteStatusArquivoResponse ws_response = ws_client.NFeLoteStatusArquivo(ws_request);
                        msgResultado = ws_response.NFeLoteStatusArquivoResult.ListaMensagemRetorno.Mensagem.ToString();
                    }
                }
                else
                if (City == "FORTALEZA")
                {
                    if (Amb == "HOM")
                        URL = @"https://isshomo.sefin.fortaleza.ce.gov.br/grpfor-iss/ServiceGinfesImplService?wsdl";
                    else
                        URL = @"https://iss.fortaleza.ce.gov.br/grpfor-iss/ServiceGinfesImplService?wsdl";

                    System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
                    System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                    textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap11, System.ServiceModel.Channels.AddressingVersion.None);
                    bnd.Elements.Add(textBindingElement);
                    System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                    httpsBindingElement.AllowCookies = true;
                    httpsBindingElement.MaxBufferSize = int.MaxValue;
                    httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                    httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
                    httpsBindingElement.RequireClientCertificate = true;
                    httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    bnd.Elements.Add(httpsBindingElement);

                    ws_fortaleza.ServiceGinfesClient ws_client = new ws_fortaleza.ServiceGinfesClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        ws_fortaleza.RecepcionarLoteRpsV3 ws_request = new ws_fortaleza.RecepcionarLoteRpsV3(Cab, XMLString);
                        ws_fortaleza.RecepcionarLoteRpsV3Response ws_response = ws_client.RecepcionarLoteRpsV3(ws_request);
                        msgResultado = ws_response.EnviarLoteRpsResposta.ToString();
                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_fortaleza.ConsultarLoteRpsV3 ws_request = new ws_fortaleza.ConsultarLoteRpsV3(Cab, XMLString);
                        ws_fortaleza.ConsultarLoteRpsV3Response ws_response = ws_client.ConsultarLoteRpsV3(ws_request);
                        msgResultado = ws_response.ConsultarLoteRpsResposta.ToString();
                    }
                }
                else
                if (City == "SAO PAULO")
                {
                    if (Amb == "HOM")
                    {
                        if (TaxReform == "YES")
                            URL = @"https://nfews.prefeitura.sp.gov.br/lotenfeasync.asmx?WSDL";
                        else
                            URL = @"https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx?WSDL";
                    }
                    else
                        URL = @"https://nfe.prefeitura.sp.gov.br/ws/lotenfe.asmx?WSDL";

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

                    ws_saopaulo.LoteNFeSoapClient ws_client = new ws_saopaulo.LoteNFeSoapClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    WS_SaoPaulo_TaxReform.LoteNFeAsyncSoapClient ws_client_taxreform = new WS_SaoPaulo_TaxReform.LoteNFeAsyncSoapClient(bnd, new EndpointAddress(URL));
                    ws_client_taxreform.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    Serializer ser = new Serializer();

                    XMLString = doc.OuterXml;

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        if (Amb == "HOM")
                        {
                            if (TaxReform == "YES")
                            {
                                WS_SaoPaulo_TaxReform.TesteEnvioLoteRPSResponseAsyncRetornoXML ws_response = new WS_SaoPaulo_TaxReform.TesteEnvioLoteRPSResponseAsyncRetornoXML();
                                ws_response = ws_client_taxreform.TesteEnvioLoteRpsAsync(2, XMLString);
                                msgResultado = ser.Serialize(ws_response);
                            }
                            else
                            {
                                ws_saopaulo.TesteEnvioLoteRPSRequest ws_request = new ws_saopaulo.TesteEnvioLoteRPSRequest(new ws_saopaulo.TesteEnvioLoteRPSRequestBody(1, XMLString));
                                ws_saopaulo.TesteEnvioLoteRPSResponse ws_response = ws_client.TesteEnvioLoteRPS(ws_request);
                                msgResultado = ws_response.Body.RetornoXML.ToString();
                            }
                        }
                        else
                        {
                            if (TaxReform == "YES")
                            {
                                WS_SaoPaulo_TaxReform.EnvioLoteRPSResponseAsyncRetornoXML ws_response = new WS_SaoPaulo_TaxReform.EnvioLoteRPSResponseAsyncRetornoXML();
                                ws_response = ws_client_taxreform.EnvioLoteRpsAsync(2, XMLString);
                                msgResultado = ser.Serialize(ws_response);
                            }
                            else
                            {
                                ws_saopaulo.EnvioLoteRPSRequest ws_request = new ws_saopaulo.EnvioLoteRPSRequest(new ws_saopaulo.EnvioLoteRPSRequestBody(1, XMLString));
                                ws_saopaulo.EnvioLoteRPSResponse ws_response = ws_client.EnvioLoteRPS(ws_request);
                                msgResultado = ws_response.Body.RetornoXML.ToString();
                            }
                        }
                    }

                    else
                    if (Action == "CONSULTA")
                    {
                        if (TaxReform == "YES")
                        {
                            WS_SaoPaulo_TaxReform.ConsultaSituacaoLoteResponseRetornoXML ws_response = new WS_SaoPaulo_TaxReform.ConsultaSituacaoLoteResponseRetornoXML();
                            ws_response = ws_client_taxreform.ConsultaSituacaoLoteSync(1, XMLString);
                            msgResultado = ser.Serialize(ws_response);
                        }
                        else
                        {
                            ws_saopaulo.ConsultaLoteRequest ws_request = new ws_saopaulo.ConsultaLoteRequest(new ws_saopaulo.ConsultaLoteRequestBody(1, XMLString));
                            ws_saopaulo.ConsultaLoteResponse ws_response = ws_client.ConsultaLote(ws_request);
                            msgResultado = ws_response.Body.RetornoXML.ToString();
                        }
                    }
                }
                else
                if (City == "RIO DE JANEIRO")
                {
                    if (Amb == "HOM")
                        URL = @"https://homologacao.notacarioca.rio.gov.br/WSNacional/nfse.asmx?wsdl";
                    else
                        URL = @"https://notacarioca.rio.gov.br/WSNacional/nfse.asmx?wsdl";

                    System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
                    System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                    textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap11, System.ServiceModel.Channels.AddressingVersion.None);
                    bnd.Elements.Add(textBindingElement);
                    System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                    httpsBindingElement.AllowCookies = true;
                    httpsBindingElement.MaxBufferSize = int.MaxValue;
                    httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                    httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
                    httpsBindingElement.RequireClientCertificate = true;
                    httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    bnd.Elements.Add(httpsBindingElement);

                    ws_riodejaneiro.NfseSoapClient ws_client = new ws_riodejaneiro.NfseSoapClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        ws_riodejaneiro.RecepcionarLoteRpsRequest ws_request = new ws_riodejaneiro.RecepcionarLoteRpsRequest(new ws_riodejaneiro.RecepcionarLoteRpsRequestBody(XMLString));
                        ws_riodejaneiro.RecepcionarLoteRpsResponse ws_response = ws_client.RecepcionarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_riodejaneiro.ConsultarLoteRpsRequest ws_request = new ws_riodejaneiro.ConsultarLoteRpsRequest(new ws_riodejaneiro.ConsultarLoteRpsRequestBody(XMLString));
                        ws_riodejaneiro.ConsultarLoteRpsResponse ws_response = ws_client.ConsultarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                }
                else
                if (City == "MACAIBA")
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

                    Serializer ser = new Serializer();

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        if (Amb == "HOM")
                        {
                            URL = @"https://www2.tinus.com.br/csp/testemac/WSNFSE.RecepcionarLoteRps.CLS";

                            ws_macaiba.RecepcionarLoteRpsSoapClient ws_client = new ws_macaiba.RecepcionarLoteRpsSoapClient(bnd, new EndpointAddress(URL));
                            ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                            ws_macaiba.RecepcionarLoteRpsRequest ws_request = new ws_macaiba.RecepcionarLoteRpsRequest();
                            ws_request = ser.Deserialize<ws_macaiba.RecepcionarLoteRpsRequest>(XMLString);

                            ws_macaiba.RecepcionarLoteRpsResponse ws_response = ws_client.RecepcionarLoteRps(ws_request);
                            msgResultado = ser.Serialize<ws_macaiba.RecepcionarLoteRpsResponse>(ws_response);

                        }
                        else
                        { 

                            URL = @"https://www.tinus.com.br/csp/macaiba/WSNFSE.RecepcionarLoteRps.CLS";

                            ws_macaiba_prod.RecepcionarLoteRpsSoapClient ws_client = new ws_macaiba_prod.RecepcionarLoteRpsSoapClient(bnd, new EndpointAddress(URL));
                            ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                            ws_macaiba_prod.RecepcionarLoteRpsRequest ws_request = new ws_macaiba_prod.RecepcionarLoteRpsRequest();
                            ws_request = ser.Deserialize<ws_macaiba_prod.RecepcionarLoteRpsRequest>(XMLString);

                            ws_macaiba_prod.RecepcionarLoteRpsResponse ws_response = ws_client.RecepcionarLoteRps(ws_request);
                            msgResultado = ser.Serialize<ws_macaiba_prod.RecepcionarLoteRpsResponse>(ws_response);

                            //TESTE PARA VERIFICAR QUAL O XML ESPERADO PELO SERVIDOR
                            //ws_macaiba_prod.RecepcionarLoteRpsRequest ws_request = new ws_macaiba_prod.RecepcionarLoteRpsRequest();
                            //ws_request.Arg = new ws_macaiba_prod.EnviarLoteRpsEnvio();
                            //ws_request.Arg.LoteRps = new ws_macaiba_prod.tcLoteRps();
                            //ws_request.Arg.LoteRps.Cnpj = "123";
                            //ws_request.Arg.LoteRps.InscricaoMunicipal = "123";
                            //msgResultado = ser.Serialize<ws_macaiba_prod.RecepcionarLoteRpsRequest>(ws_request);

                        }

                    }
                    else
                    if (Action == "CONSULTA")
                    {

                        if (Amb == "HOM")
                        {
                            URL = @"https://www2.tinus.com.br/csp/testemac/WSNFSE.ConsultarLoteRps.CLS";

                            ws_macaiba_cons_alt.ConsultarLoteRpsSoapClient ws_client = new ws_macaiba_cons_alt.ConsultarLoteRpsSoapClient(bnd, new EndpointAddress(URL));
                            ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                            ws_macaiba_cons_alt.ConsultarLoteRpsRequest ws_request = new ws_macaiba_cons_alt.ConsultarLoteRpsRequest();
                            ws_request = ser.Deserialize<ws_macaiba_cons_alt.ConsultarLoteRpsRequest>(XMLString);

                            ws_macaiba_cons_alt.ConsultarLoteRpsResponse ws_response = ws_client.ConsultarLoteRps(ws_request);
                            msgResultado = ser.Serialize<ws_macaiba_cons_alt.ConsultarLoteRpsResponse>(ws_response);

                        }
                        else
                        { 
                            URL = @"https://www.tinus.com.br/csp/macaiba/WSNFSE.ConsultarLoteRps.CLS";

                            ws_macaiba_cons_prod_alt.ConsultarLoteRpsSoapClient ws_client = new ws_macaiba_cons_prod_alt.ConsultarLoteRpsSoapClient(bnd, new EndpointAddress(URL));
                            ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                            ws_macaiba_cons_prod_alt.ConsultarLoteRpsRequest ws_request = new ws_macaiba_cons_prod_alt.ConsultarLoteRpsRequest();
                            ws_request = ser.Deserialize<ws_macaiba_cons_prod_alt.ConsultarLoteRpsRequest>(XMLString);

                            ws_macaiba_cons_prod_alt.ConsultarLoteRpsResponse ws_response = ws_client.ConsultarLoteRps(ws_request);
                            msgResultado = ser.Serialize<ws_macaiba_cons_prod_alt.ConsultarLoteRpsResponse>(ws_response);

                        }
                    }

                }
                else
                if (City == "BELO HORIZONTE")
                {
                    if (Amb == "HOM")
                        URL = @"https://bhisshomologa.pbh.gov.br/bhiss-ws/nfse?wsdl";
                    else
                        URL = @"https://bhissdigital.pbh.gov.br/bhiss-ws/nfse?wsdl";

                    System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
                    System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                    textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap11, System.ServiceModel.Channels.AddressingVersion.None);
                    bnd.Elements.Add(textBindingElement);
                    System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                    httpsBindingElement.AllowCookies = true;
                    httpsBindingElement.MaxBufferSize = int.MaxValue;
                    httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                    httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
                    httpsBindingElement.RequireClientCertificate = true;
                    httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    bnd.Elements.Add(httpsBindingElement);

                    ws_bh.nfseClient ws_client = new ws_bh.nfseClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        ws_bh.RecepcionarLoteRpsRequest ws_request = new ws_bh.RecepcionarLoteRpsRequest(new ws_bh.RecepcionarLoteRpsRequestBody(Cab, XMLString));
                        ws_bh.RecepcionarLoteRpsResponse ws_response = ws_client.RecepcionarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_bh.ConsultarLoteRpsRequest ws_request = new ws_bh.ConsultarLoteRpsRequest(new ws_bh.ConsultarLoteRpsRequestBody(Cab, XMLString));
                        ws_bh.ConsultarLoteRpsResponse ws_response = ws_client.ConsultarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                }
                else
                if (City == "BRASILIA")
                {
                    if (Amb == "HOM")
                        URL = @"https://bhisshomologa.pbh.gov.br/bhiss-ws/nfse?wsdl";
                    else
                        URL = @"https://bhissdigital.pbh.gov.br/bhiss-ws/nfse?wsdl";

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

                    ws_belohorizonte.nfseClient ws_client = new ws_belohorizonte.nfseClient(bnd, new EndpointAddress(URL));
                    ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        ws_belohorizonte.RecepcionarLoteRpsRequest ws_request = new ws_belohorizonte.RecepcionarLoteRpsRequest(new ws_belohorizonte.RecepcionarLoteRpsRequestBody(Cab, XMLString));
                        ws_belohorizonte.RecepcionarLoteRpsResponse ws_response = ws_client.RecepcionarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_belohorizonte.ConsultarLoteRpsRequest ws_request = new ws_belohorizonte.ConsultarLoteRpsRequest(new ws_belohorizonte.ConsultarLoteRpsRequestBody(Cab, XMLString));
                        ws_belohorizonte.ConsultarLoteRpsResponse ws_response = ws_client.ConsultarLoteRps(ws_request);
                        msgResultado = ws_response.Body.outputXML.ToString();
                    }
                }
                else
                if (City == "OSASCO")
                {

                    if (Amb == "HOM")
                        URL = @"https://nfe.osasco.sp.gov.br/EISSNFEWebServices/NotaFiscalEletronica.svc?Singlewsdl";
                    else
                        URL = @"https://nfe.osasco.sp.gov.br/EISSNFEWebServices/NotaFiscalEletronica.svc?Singlewsdl";

                    Serializer ser = new Serializer();
                    ws_osasco.NotaFiscalEletronicaServicoClient ws_client = new ws_osasco.NotaFiscalEletronicaServicoClient(0, URL);

                    if (Action == "RECEPCIONARLOTERPS")
                    {

                        ws_osasco.EmitirRequest ws_request = new ws_osasco.EmitirRequest();
                        ws_request = ser.Deserialize<ws_osasco.EmitirRequest>(XMLString);

                        ws_osasco.EmitirResponse ws_response = ws_client.Emitir(ws_request);
                        msgResultado = ser.Serialize<ws_osasco.EmitirResponse>(ws_response);

                    }
                    else
                    if (Action == "CONSULTA")
                    {

                        ws_osasco.ConsultarRequest ws_request = new ws_osasco.ConsultarRequest();
                        ws_request = ser.Deserialize<ws_osasco.ConsultarRequest>(XMLString);

                        ws_osasco.ConsultarResponse ws_response = ws_client.Consultar(ws_request);
                        msgResultado = ser.Serialize<ws_osasco.ConsultarResponse>(ws_response);

                    }
                }
                else
                if (City == "CAMPINAS")
                {

                    URL = @"http://issdigital.campinas.sp.gov.br/WsNFe2/LoteRps.jws?wsdl";

                    Serializer ser = new Serializer();
                    ws_campinas.LoteRpsClient ws_client = new ws_campinas.LoteRpsClient(0, URL);

                    if (Action == "RECEPCIONARLOTERPS")
                    {

                        if (Amb == "HOM") 
                        {
                            ws_campinas.testeEnviarRequest ws_request = new ws_campinas.testeEnviarRequest();
                            ws_request = ser.Deserialize<ws_campinas.testeEnviarRequest>(XMLString);

                            ws_campinas.testeEnviarResponse ws_response = ws_client.testeEnviar(ws_request);
                            msgResultado = ser.Serialize<ws_campinas.testeEnviarResponse>(ws_response);
                        }
                        else
                        {
                            ws_campinas.enviarRequest ws_request = new ws_campinas.enviarRequest();
                            ws_request = ser.Deserialize<ws_campinas.enviarRequest>(XMLString);

                            ws_campinas.enviarResponse ws_response = ws_client.enviar(ws_request);
                            msgResultado = ser.Serialize<ws_campinas.enviarResponse>(ws_response);
                        }

                    }
                    else
                    if (Action == "CONSULTA")
                    {
                        ws_campinas.consultarLoteRequest ws_request = new ws_campinas.consultarLoteRequest();
                        ws_request = ser.Deserialize<ws_campinas.consultarLoteRequest>(XMLString);

                        ws_campinas.consultarLoteResponse ws_response = ws_client.consultarLote(ws_request);
                        msgResultado = ser.Serialize<ws_campinas.consultarLoteResponse>(ws_response);
                    }
                }
                else
                if (City == "SAOCAETANO")
                {

                    if (Amb == "HOM")
                        URL = @"https://ws-homologacao-rtc.giss.com.br/service-ws/nf/nfse-ws?wsdl";
                    else
                        URL = @"https://ws-scs.giss.com.br/service-ws/nf/nfse-ws?wsdl";

                    System.ServiceModel.Channels.CustomBinding bnd = new System.ServiceModel.Channels.CustomBinding();
                    System.ServiceModel.Channels.TextMessageEncodingBindingElement textBindingElement = new System.ServiceModel.Channels.TextMessageEncodingBindingElement();
                    textBindingElement.MessageVersion = System.ServiceModel.Channels.MessageVersion.CreateVersion(System.ServiceModel.EnvelopeVersion.Soap11, System.ServiceModel.Channels.AddressingVersion.None);
                    bnd.Elements.Add(textBindingElement);
                    System.ServiceModel.Channels.HttpsTransportBindingElement httpsBindingElement = new System.ServiceModel.Channels.HttpsTransportBindingElement();
                    httpsBindingElement.AllowCookies = true;
                    httpsBindingElement.MaxBufferSize = int.MaxValue;
                    httpsBindingElement.MaxReceivedMessageSize = int.MaxValue;
                    httpsBindingElement.AuthenticationScheme = AuthenticationSchemes.Digest;
                    httpsBindingElement.RequireClientCertificate = true;
                    httpsBindingElement.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    bnd.Elements.Add(httpsBindingElement);

                    if (Action == "RECEPCIONARLOTERPS")
                    {
                        X509Certificate2 _X509Cert = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                        //assina o RPS caso não esteja assinado
                        XMLString = SignNFSe_LoteRps.EnsureSigned(XMLString, _X509Cert);

                        wsSaoCaetano.nfseClient ws_client = new wsSaoCaetano.nfseClient(bnd, new EndpointAddress(URL));
                        ws_client.ClientCredentials.ClientCertificate.Certificate = _X509Cert;

                        //soapLog = SoapEnvelopeLogger.AttachTo(ws_client, "saocaetano-recepcionarloterps");

                        wsSaoCaetano.RecepcionarLoteRps ws_request = new wsSaoCaetano.RecepcionarLoteRps(Cab, XMLString);
                        msgResultado = ws_client.RecepcionarLoteRps(ws_request.nfseCabecMsg, ws_request.nfseDadosMsg);
                    }

                    else
                    if (Action == "CONSULTA")
                    {

                        wsSaoCaetano.nfseClient ws_client = new wsSaoCaetano.nfseClient(bnd, new EndpointAddress(URL));
                        ws_client.ClientCredentials.ClientCertificate.Certificate = new X509Certificate2(Convert.FromBase64String(Cert), Pass, X509KeyStorageFlags.MachineKeySet);

                        wsSaoCaetano.ConsultarLoteRps ws_request = new wsSaoCaetano.ConsultarLoteRps(Cab, XMLString);
                        msgResultado = ws_client.ConsultarLoteRps(ws_request.nfseCabecMsg, ws_request.nfseDadosMsg);
                    }
                }
            }
            else
            {
                error = true;
                msgResultado = City + " - " + Action + ": not implemented yet";
            }

        }
        catch (Exception caught)
        {
            var rootCause = caught.GetBaseException();
            msgResultado = "Erro: " + rootCause.Message;
            error = true;
        }

        //if (debug && soapLog != null)
        //    msgResultado = soapLog.Describe() + msgResultado;

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);

    }

}

public class Serializer
{
    public T Deserialize<T>(string input) where T : class
    {
        System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

        using (StringReader sr = new StringReader(input))
        {
            return (T)ser.Deserialize(sr);
        }
    }

    public string Serialize<T>(T ObjectToSerialize)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());

        using (StringWriter textWriter = new StringWriter())
        {
            xmlSerializer.Serialize(textWriter, ObjectToSerialize);
            return textWriter.ToString();
        }
    }
}

