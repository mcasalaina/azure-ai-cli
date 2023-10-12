// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.AI.OpenAI;

namespace Azure.AI.Chat.SampleService.Services;

public class OpenAIClientProvider : IOpenAIClientProvider
{
    private readonly string? _azureOpenAIKey;
    private readonly string? _azureOpenAIEndpoint;
    private readonly string? _azureOpenAIDeployment;

    public OpenAIClientProvider()
    {
        _azureOpenAIKey = Environment.GetEnvironmentVariable("CHAT_AZURE_OPEN_AI_KEY");
        _azureOpenAIEndpoint = Environment.GetEnvironmentVariable("CHAT_AZURE_OPEN_AI_ENDPOINT");
        _azureOpenAIDeployment = Environment.GetEnvironmentVariable("CHAT_AZURE_OPEN_AI_DEPLOYMENT");

    }

    public OpenAIClient GetClient()
    {
        if (_azureOpenAIKey == null || _azureOpenAIEndpoint == null)
        {
            throw new InvalidOperationException("Azure Open AI key and endpoint must be set");
        }
        return new OpenAIClient(new(_azureOpenAIEndpoint), new AzureKeyCredential(_azureOpenAIKey));
    }

    public string GetDeployment()
    {
        if (_azureOpenAIDeployment == null)
        {
            throw new InvalidOperationException("Azure Open AI deployment must be set");
        }
        return _azureOpenAIDeployment;
    }
}