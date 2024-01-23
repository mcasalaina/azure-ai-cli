using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class WolframAlphaHelper
{
    private static readonly HttpClient client = new HttpClient();

    [HelperFunctionDescription("Calls Wolfram Alpha to answer a calculus question")]
    public static async Task<string> AnswerCalculusQuestion(string question)
    {
        string wolframAlphaApiKey = "JJVEU8-J2TE2U24G2";
        string requestUri = $"http://api.wolframalpha.com/v2/query?input={Uri.EscapeDataString(question)}&appid={wolframAlphaApiKey}";

        HttpResponseMessage response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
}