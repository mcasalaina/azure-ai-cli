//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using Azure.AI.Details.Common.CLI.ConsoleGui;

namespace Azure.AI.Details.Common.CLI
{
    public partial class AzCliConsoleGui
    {
        public static async Task<AzCli.AccountRegionLocationInfo> PickRegionLocationAsync(bool interactive, string regionFilter = null, bool allowAnyRegionOption = true)
        {
            var regionLocation = await FindRegionAsync(interactive, regionFilter, allowAnyRegionOption);
            if (regionLocation == null)
            {
                throw new ApplicationException($"CANCELED: No resource region/location selected.");
            }
            return regionLocation.Value;
        }

        public static async Task<AzCli.AccountRegionLocationInfo?> FindRegionAsync(bool interactive, string regionFilter = null, bool allowAnyRegionOption = false)
        {
            var p0 = allowAnyRegionOption ? "(Any region/location)" : null;
            var hasP0 = !string.IsNullOrEmpty(p0);

            Console.Write("\rRegion: *** Loading choices ***");
            var response = await AzCli.ListAccountRegionLocations();

            Console.Write("\rRegion: ");
            if (string.IsNullOrEmpty(response.Output.StdOutput) && !string.IsNullOrEmpty(response.Output.StdError))
            {
                throw new ApplicationException($"ERROR: Loading resource region/locations\n{response.Output.StdError}");
            }

            var regions = response.Payload
                .Where(x => MatchRegionLocationFilter(x, regionFilter))
                .OrderBy(x => x.RegionalDisplayName)
                .ToList();

            var exactMatch = regionFilter != null && regions.Count(x => ExactMatchRegionLocation(x, regionFilter)) == 1;
            if (exactMatch) regions = regions.Where(x => ExactMatchRegionLocation(x, regionFilter)).ToList();

            if (regions.Count() == 0)
            {
                ConsoleHelpers.WriteLineError(response.Payload.Count() > 0
                    ? "*** No matching resource region/locations found ***"
                    : "*** No resource region/locations found ***");
                return null;
            }
            else if (regions.Count() == 1 && (!interactive || exactMatch))
            {
                var region = regions.First();
                DisplayNameAndDisplayName(region);
                return region;
            }
            else if (!interactive)
            {
                ConsoleHelpers.WriteLineError("*** More than 1 region/location found ***");
                Console.WriteLine();
                DisplayRegionLocations(regions, "  ");
                return null;
            }

            return interactive
                ? ListBoxPickAccountRegionLocation(regions.ToArray(), p0)
                : null;
        }

        private static AzCli.AccountRegionLocationInfo? ListBoxPickAccountRegionLocation(AzCli.AccountRegionLocationInfo[] items, string p0)
        {
            var list = items.Select(x => x.RegionalDisplayName).ToList();

            var hasP0 = !string.IsNullOrEmpty(p0);
            if (hasP0) list.Insert(0, p0);

            var picked = ListBoxPicker.PickIndexOf(list.ToArray());
            if (picked < 0)
            {
                return null;
            }

            if (hasP0 && picked == 0)
            {
                Console.WriteLine(p0);
                return new AzCli.AccountRegionLocationInfo();
            }

            if (hasP0) picked--;
            var regionLocation = items[picked];

            DisplayNameAndDisplayName(regionLocation);
            return regionLocation;
        }

        static bool ExactMatchRegionLocation(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            return regionLocation.Name.ToLower() == regionLocationFilter || regionLocation.DisplayName == regionLocationFilter || regionLocation.RegionalDisplayName == regionLocationFilter;
        }

        private static bool MatchRegionLocationFilter(AzCli.AccountRegionLocationInfo regionLocation, string regionLocationFilter)
        {
            if (regionLocationFilter == null || ExactMatchRegionLocation(regionLocation, regionLocationFilter))
            {
                return true;
            }

            var displayName = regionLocation.DisplayName.ToLower();
            var regionalName = regionLocation.RegionalDisplayName.ToLower();

            return displayName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(displayName, regionLocationFilter) ||
                regionalName.Contains(regionLocationFilter) || StringHelpers.ContainsAllCharsInOrder(regionalName, regionLocationFilter);
        }

        private static void DisplayRegionLocations(List<AzCli.AccountRegionLocationInfo> regionLocations, string prefix)
        {
            foreach (var regionLocation in regionLocations)
            {
                Console.Write(prefix);
                DisplayNameAndDisplayName(regionLocation);
            }
        }

        private static void DisplayNameAndDisplayName(AzCli.AccountRegionLocationInfo regionLocation)
        {
            Console.Write($"{regionLocation.DisplayName} ({regionLocation.Name})                  ");
            Console.WriteLine(new string(' ', 20));
        }
    }
}
