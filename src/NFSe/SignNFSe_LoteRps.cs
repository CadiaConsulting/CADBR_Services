using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

/// <summary>
/// Assinatura do lote no layout ABRASF 2.04 (usado por Sao Caetano do Sul / GISS).
///
/// O XSD posiciona duas assinaturas distintas:
///
///   EnviarLoteRpsEnvio
///    |- LoteRps (Id)
///    |   \- ListaRps
///    |       \- Rps
///    |           |- InfDeclaracaoPrestacaoServico (Id)
///    |           \- Signature       assinatura do RPS
///    \- Signature                   assinatura do lote
///
/// A ordem importa: o RPS e assinado primeiro, porque a assinatura do lote cobre
/// todo o LoteRps, incluindo as assinaturas de RPS ja inseridas.
/// </summary>
public static class SignNFSe_LoteRps
{
    private const string DSIG = "http://www.w3.org/2000/09/xmldsig#";

    /// <summary>
    /// Devolve o lote assinado. Assinaturas que ja vierem no XML sao preservadas:
    /// so e assinado o que ainda nao estiver.
    /// </summary>
    public static string EnsureSigned(string xml, X509Certificate2 certificate)
    {
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = false;
        doc.LoadXml(xml);

        XmlElement root = doc.DocumentElement
            ?? throw new InvalidOperationException("XML do lote vazio.");

        XmlElement lote = SelectOne(doc, "//*[local-name()='LoteRps']", "LoteRps");

        //cada RPS primeiro: a assinatura do lote precisa cobri-las
        XmlNodeList? rpsList = doc.SelectNodes("//*[local-name()='ListaRps']/*[local-name()='Rps']");

        if (rpsList != null)
        {
            foreach (XmlNode rps in rpsList)
            {
                XmlElement rpsElement = (XmlElement)rps;

                if (HasSignatureChild(rpsElement))
                    continue;

                XmlElement inf = SelectOne(rpsElement,
                    "./*[local-name()='InfDeclaracaoPrestacaoServico']", "InfDeclaracaoPrestacaoServico");

                Sign(doc, inf, rpsElement, certificate);
            }
        }

        if (!HasSignatureChild(root))
            Sign(doc, lote, root, certificate);

        return doc.OuterXml;
    }

    /// <summary>True se a tag ja tem uma assinatura como filha direta.</summary>
    public static bool IsSigned(string xml)
    {
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = false;
        doc.LoadXml(xml);

        return doc.DocumentElement != null && HasSignatureChild(doc.DocumentElement);
    }

    private static void Sign(XmlDocument doc, XmlElement elementToSign, XmlElement parentForSignature,
        X509Certificate2 certificate)
    {
        string id = elementToSign.GetAttribute("Id");

        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException(
                "A tag " + elementToSign.LocalName + " nao tem atributo Id, exigido para a assinatura.");

        Reference reference = new Reference("#" + id);
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());

        SignedXml signedXml = new SignedXml(doc);
        signedXml.SigningKey = certificate.GetRSAPrivateKey();
        signedXml.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.AddReference(reference);

        signedXml.KeyInfo = new KeyInfo();
        signedXml.KeyInfo.AddClause(new KeyInfoX509Data(certificate));

        signedXml.ComputeSignature();

        parentForSignature.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
    }

    private static bool HasSignatureChild(XmlElement parent)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.LocalName == "Signature" && child.NamespaceURI == DSIG)
                return true;
        }

        return false;
    }

    private static XmlElement SelectOne(XmlNode context, string xpath, string nomeTag)
    {
        XmlNode? node = context.SelectSingleNode(xpath);

        if (node is not XmlElement element)
            throw new InvalidOperationException("A tag " + nomeTag + " nao foi encontrada no lote.");

        return element;
    }
}
