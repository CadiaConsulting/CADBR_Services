using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;
using HttpTriggerAttribute = Microsoft.Azure.Functions.Worker.HttpTriggerAttribute;



public class ConvertInfRespTec
{
    private readonly ILogger<ConvertInfRespTec> _logger;

    public ConvertInfRespTec(ILogger<ConvertInfRespTec> logger)
    {
        _logger = logger;
    }

    [Function("ConvertInfRespTec")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req)
    {

        string msgResultado = "";
        bool error;

        try
        {
            string HashCSRT = req.Headers["HashCSRT"];
            var bytetxt = System.Text.Encoding.UTF8.GetBytes(HashCSRT);
            var SHA1 = new SHA1Managed();
            byte[] hash = SHA1.ComputeHash(bytetxt);
            msgResultado = Convert.ToBase64String(hash);
            error = false;
        }
        catch (Exception caught)
        {
            msgResultado = "Erro: " + caught.Message.ToString();
            error = true;
        }

        return !error
            ? (ActionResult)new OkObjectResult(msgResultado)
            : new BadRequestObjectResult(msgResultado);
    }
}
