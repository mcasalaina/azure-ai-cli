//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Globalization;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<(AzCli.CognitiveServicesDeploymentInfo?,bool)> PickOrCreateCognitiveServicesResourceDeployment(bool interactive, bool allowSkipDeployment, string deploymentExtra, string subscriptionId, string groupName, string resourceRegionLocation, string resourceName, string deploymentFilter, string modelFilter)
        {
            bool createdNew = false;
            ConsoleHelpers.WriteLineWithHighlight($"\n`AZURE OPENAI DEPLOYMENT ({deploymentExtra.ToUpper()})`");

            var createNewItem = !string.IsNullOrEmpty(deploymentFilter)
                ? $"(Create `{deploymentFilter}`)"
                : interactive ? "(Create new)" : null;

            var deployment = await FindCognitiveServicesResourceDeployment(interactive, allowSkipDeployment, deploymentExtra, subscriptionId, groupName, resourceName, deploymentFilter, createNewItem);
            if (deployment == null && allowSkipDeployment)
                return (null, createdNew);

            if (deployment != null && deployment.Value.Name == null)
            {
                createdNew = true;
                deployment = await TryCreateCognitiveServicesResourceDeployment(interactive, deploymentExtra, subscriptionId, groupName, resourceRegionLocation, resourceName, deploymentFilter, modelFilter);
            }

            if (deployment == null)
            {
                throw new ApplicationException($"CANCELED: No deployment selected");
            }

            return (deployment, createdNew);
        }

        public static async Task<AzCli.CognitiveServicesDeploymentInfo?> FindCognitiveServicesResourceDeployment(bool interactive, bool allowSkipDeployment, string deploymentExtra, string subscriptionId, string groupName, string resourceName, string deploymentFilter, string allowCreateDeploymentOption)
        {
            var allowCreateDeployment = !string.IsNullOrEmpty(allowCreateDeploymentOption);

            var response = await Program.CognitiveServicesClient.GetAllDeploymentsAsync(subscriptionId, groupName, resourceName, Program.CancelToken);
            response.ThrowOnFail("Loading Cognitive Services deployments");

            var lookForChatCompletionCapable = deploymentExtra.ToLower() == "chat" || deploymentExtra.ToLower() == "evaluation";
            var lookForEmbeddingCapable = deploymentExtra.ToLower() == "embeddings";

            var deployments = response.Value
                .Where(x => MatchDeploymentFilter(x, deploymentFilter))
                .Where(x => !lookForChatCompletionCapable || x.ChatCompletionCapable)
                .Where(x => !lookForEmbeddingCapable || x.EmbeddingsCapable)
                .OrderBy(x => x.Name)
                .ToList();

            var exactMatch = deploymentFilter != null && deployments.Count(x => ExactMatchDeployment(x, deploymentFilter)) == 1;
            if (exactMatch) deployments = deployments.Where(x => ExactMatchDeployment(x, deploymentFilter)).ToList();

            if (deployments.Count() == 0)
            {
                if (!allowCreateDeployment)
                {
                    ConsoleHelpers.WriteLineError($"*** No deployments found ***");
                    return null;
                }
                else if (!interactive)
                {
                    Console.WriteLine(allowCreateDeploymentOption);
                    return new AzCli.CognitiveServicesDeploymentInfo();
                }
            }
            else if (deployments.Count() == 1 && (!interactive || exactMatch))
            {
                var deployment = deployments.First();
                DisplayName(deployment);
                return deployment;
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 deployment found ***");
                Console.WriteLine();
                DisplayDeployments(deployments, "  ");
                return null;
            }

            var scanFor = deploymentExtra.ToLower() switch {
                "chat" => "gpt",
                "embeddings" => "embedding",
                "evaluation" => "gpt",
                _ => deploymentExtra.ToLower()
            };

            var choices = deployments.ToArray();
            var select = Array.FindIndex(choices, x => x.Name.Contains(scanFor));

            if (allowCreateDeployment && select >= 0) select++;
            select = Math.Max(0, select);

            return interactive
                ? ListBoxPickDeployment(choices, allowCreateDeploymentOption, allowSkipDeployment, select)
                : null;
        }

        public static async Task<AzCli.CognitiveServicesDeploymentInfo?> TryCreateCognitiveServicesResourceDeployment(bool interactive, string deploymentExtra, string subscriptionId, string groupName, string resourceRegionLocation, string resourceName, string deploymentName, string modelFilter)
        {
            ConsoleHelpers.WriteLineWithHighlight($"\n`CREATE DEPLOYMENT ({deploymentExtra.ToUpper()})`");

            var deployableModel = await FindDeployableModel(interactive, deploymentExtra, subscriptionId, resourceRegionLocation, modelFilter);
            if (deployableModel == null) return null;

            var modelName = deployableModel?.Name;
            Console.WriteLine($"\rModel: {modelName}");

            var modelFormat = "OpenAI";
            var modelVersion = deployableModel?.Version;
            var scaleCapacity = deployableModel?.DefaultCapacity ?? 0;

            Console.Write("\rName: ");
            if (!string.IsNullOrEmpty(deploymentName))
            {
                Console.WriteLine(deploymentName);
            }
            else if (interactive)
            {
                var choices = new string[] {
                    $"{modelName}-{modelVersion}",
                    "(Enter custom name)"
                };
                var pick = ListBoxPicker.PickIndexOf(choices);
                deploymentName = pick switch
                {
                    0 => $"{modelName}-{modelVersion}",
                    1 => AskPromptHelper.AskPrompt("\rName: "),
                    _ => null
                };
                if (pick != choices.Length - 1)
                {
                    Console.WriteLine($"\rName: {deploymentName}");
                }
            }
            else
            {
                deploymentName = $"{modelName}-{modelVersion}";
                Console.WriteLine(deploymentName);
            }

            if (string.IsNullOrEmpty(deploymentName)) return null;

            Console.Write("*** CREATING ***");
            var response = await Program.CognitiveServicesClient.CreateDeploymentAsync(subscriptionId, groupName, resourceName, deploymentName, modelName, modelVersion, modelFormat, scaleCapacity.ToString(CultureInfo.InvariantCulture), Program.CancelToken);

            Console.Write("\r");
            response.ThrowOnFail("Creating deployment");

            Console.WriteLine("\r*** CREATED ***  ");
            return response.Value;
        }

        private static async Task<AzCli.CognitiveServicesModelInfo?> FindDeployableModel(bool interactive, string deploymentExtra, string subscriptionId, string resourceRegionLocation, string modelFilter)
        {
            Console.Write("\rModel: *** Loading choices ***");
            var models = await Program.CognitiveServicesClient.GetAllModelsAsync(subscriptionId, resourceRegionLocation, Program.CancelToken);
            var usage = await Program.CognitiveServicesClient.GetAllModelUsageAsync(subscriptionId, resourceRegionLocation, Program.CancelToken);

            models.ThrowOnFail("Loading models");
            usage.ThrowOnFail("Loading model usage");

            var lookForChatCompletionCapable = deploymentExtra.ToLower() == "chat" || deploymentExtra.ToLower() == "evaluation";
            var lookForEmbeddingCapable = deploymentExtra.ToLower() == "embeddings";
            var capableModels = models.Value
                .Where(x => !lookForChatCompletionCapable || x.IsChatCapable)
                .Where(x => !lookForEmbeddingCapable || x.IsEmbeddingsCapable)
                .ToArray();

            Console.Write("\rModel: ");
            var deployableModels = FilterModelsByUsage(capableModels, usage.Value);

            var exactMatch = modelFilter != null && deployableModels.Count(x => ExactMatchModel(x, modelFilter)) == 1;
            if (exactMatch) deployableModels = deployableModels.Where(x => ExactMatchModel(x, modelFilter)).ToArray();

            if (deployableModels.Count() == 0)
            {
                ConsoleHelpers.WriteLineError(models.Value.Count() > 0
                    ? $"*** No matching {deploymentExtra} capable models with capacity found ***"
                    : "*** No deployable models found ***");
                return null;
            }
            else if (deployableModels.Count() == 1 && (!interactive || exactMatch))
            {
                var model = deployableModels.First();
                Console.WriteLine($"{model.Name} (version {model.Version})");
                return model;
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 deployable model found ***");
                Console.WriteLine();
                DisplayDeployableModels(deployableModels.ToList(), "  ");
                return null;
            }

            var choices = Program.Debug
                ? deployableModels.Select(x => x.Name + " (version " + x.Version + ")" + $"{x.DefaultCapacity} capacity").ToArray()
                : deployableModels.Select(x => x.Name + " (version " + x.Version + ")").ToArray();

            var scanFor = deploymentExtra.ToLower() switch
            {
                "chat" => "gpt",
                "embeddings" => "embedding",
                _ => deploymentExtra.ToLower()
            };
            var select = Math.Max(0, Array.FindLastIndex(choices, x => x.Contains(scanFor)));

            var index = ListBoxPicker.PickIndexOf(choices, select);
            if (index < 0) return null;

            return deployableModels[index];
        }

        private static void DisplayDeployableModels(List<AzCli.CognitiveServicesModelInfo> deployableModels, string prefix)
        {
            foreach (var deployableModel in deployableModels)
            {
                Console.Write(prefix);
                DisplayNameAndVersion(deployableModel);
            }
            Console.WriteLine();
        }

        private static void DisplayNameAndVersion(AzCli.CognitiveServicesModelInfo deployableModel)
        {
            Console.WriteLine($"{deployableModel.Name}-{deployableModel.Version}");
        }

        private static bool ExactMatchModel(AzCli.CognitiveServicesModelInfo model, string modelFilter)
        {
            var displayName = model.Name + "-" + model.Version;
            return displayName.ToLower() == modelFilter.ToLower();
        }

        private static AzCli.CognitiveServicesModelInfo[] FilterModelsByUsage(AzCli.CognitiveServicesModelInfo[] models, AzCli.CognitiveServicesUsageInfo[] usage)
        {
            models = models.GroupBy(x => x.Name + x.Version + x.Format).Select(x => x.First()).ToArray();
            
            if (Program.Debug)
            {
                Console.WriteLine($"\rModel: (found {models.Count()} models)\n");
                foreach (var model in models)
                {
                    Console.WriteLine($"{model.Name} (version {model.Version}) capacity={model.DefaultCapacity}");
                }
                Console.WriteLine();
            }

            var filteredKeep = new List<AzCli.CognitiveServicesModelInfo>();
            foreach (var model in models)
            {
                var defaultCapacityValue = (float)model.DefaultCapacity;

                var checkUsage = usage.Where(x => x.Name.Value.EndsWith(model.Name));
                var current = checkUsage.Sum(x => x.CurrentValue);
                var limit = checkUsage.Sum(x => x.Limit);

                var available = limit - current;
                if (available <= 0) continue;

                if (defaultCapacityValue <= available)
                {
                    filteredKeep.Add(model);
                    continue;
                }

                var newDefault = available - 1;
                if (newDefault < 1) newDefault = 1;

                filteredKeep.Add(model);
            }

            if (filteredKeep.Count() <= models.Count())
            {
                var filteredDidntKeep = new List<string>();
                foreach (var model in models)
                {
                    if (filteredKeep.Any(x => x.Name == model.Name && x.Version == model.Version && x.Format == model.Format)) continue;
                    filteredDidntKeep.Add($"{model.Name} (version {model.Version})");
                }
                filteredDidntKeep.Sort();

                if (filteredDidntKeep.Count() > 0)
                {
                    Console.WriteLine($"\rModel: (excluded {filteredDidntKeep.Count()} models with zero remaining quota)\n");
                    foreach (var model in filteredDidntKeep)
                    {
                        ConsoleHelpers.WriteLineWithHighlight($"  `#e_;*** EXCLUDED: {model} ***`");
                    }
                    Console.WriteLine();
                }
            }

            if (Program.Debug)
            {
                Console.WriteLine($"\rModel: ({filteredKeep.Count()} models with remaining quota)\n");
                foreach (var model in filteredKeep)
                {
                    Console.WriteLine($"{model.Name} (version {model.Version}) capacity={model.DefaultCapacity}");
                }
                Console.WriteLine();
            }

            return filteredKeep.ToArray();
        }

        private static AzCli.CognitiveServicesDeploymentInfo? ListBoxPickDeployment(AzCli.CognitiveServicesDeploymentInfo[] deployments, string p0, bool allowSkipDeployment = false, int select = 0)
        {
            var list = deployments.Select(x => $"{x.Name} ({x.ModelName})").ToList();
         
            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            if (allowSkipDeployment)
            {
                list.Add("(Skip)");
            }

            var picked = ListBoxPicker.PickIndexOf(list.ToArray(), select);
            if (picked < 0)
            {
                Console.WriteLine();
                return null;
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return new AzCli.CognitiveServicesDeploymentInfo();
            }

            if (allowSkipDeployment && picked == list.Count - 1)
            {
                Console.WriteLine("(Skip)");
                return null;
            }

            if (hasP0) picked--;
            Console.WriteLine($"{deployments[picked].Name}");
            return deployments[picked];
        }

        private static bool ExactMatchDeployment(AzCli.CognitiveServicesDeploymentInfo deployment, string deploymentFilter)
        {
            return !string.IsNullOrEmpty(deploymentFilter) && deployment.Name.ToLower() == deploymentFilter;
        }

        private static bool MatchDeploymentFilter(AzCli.CognitiveServicesDeploymentInfo deployment, string deploymentFilter)
        {
            if (deploymentFilter != null && ExactMatchDeployment(deployment, deploymentFilter))
            {
                return true;
            }

            var name = deployment.Name.ToLower();
            return (string.IsNullOrEmpty(deploymentFilter) || name.Contains(deploymentFilter) || StringHelpers.ContainsAllCharsInOrder(name, deploymentFilter));
        }

        private static void DisplayDeployments(List<AzCli.CognitiveServicesDeploymentInfo> deployments, string prefix)
        {
            foreach (var deployment in deployments)
            {
                Console.Write(prefix);
                DisplayName(deployment);
            }
        }

        private static void DisplayName(AzCli.CognitiveServicesDeploymentInfo deployment)
        {
            Console.Write($"{deployment.Name}");
            Console.WriteLine(new string(' ', 20));
        }
    }
}
