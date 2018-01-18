using System;
using System.Collections.Generic;
using System.Text;
using SmarterBalanced.SampleItems.Dal.Providers.Models;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.IO;
using SmarterBalanced.SampleItems.Dal.Providers;
using Microsoft.Extensions.Logging;
using System.Linq;
using SmarterBalanced.SampleItems.Dal.Translations;

namespace SmarterBalanced.SampleItems.Core.AccessibilityTesting
{
    public class AccessibilityTestRepo
    {
        private readonly SampleItemsContext context;
        private readonly ILogger logger;

        public AccessibilityTestRepo(SampleItemsContext context, ILoggerFactory loggerFactory)
        {
            this.context = context;
            logger = loggerFactory.CreateLogger<AccessibilityTestRepo>();
        }

        public IList<AccessibilityFamilySearchResult> GetAccessibilityFamilies(string accessibilityResource)
        {
            ImmutableArray<MergedAccessibilityFamily> families = context.MergedAccessibilityFamilies
                .Where(af => af.Resources
                .Any(ar => ar.Label.Contains(accessibilityResource))).ToImmutableArray();

            var results = families.Select(fam => AccessibilityFamilySearchResult.FromMergedFamily(fam)).ToList();

            return results;
        }

        public IList<ItemCardViewModel> GetAccessibilityItems()
        {
           var items = context.SampleItems
                .Where(t => t.AccessibilityResourceGroups
                .Any(ar => ar.AccessibilityResources.Any(ac => (ac.Label == "English Dictionary" && ac.Disabled == false)))).Take(10);
            
            var cards = items.Select(i => i.ToItemCardViewModel()).ToList();
            
            return cards;
        }

        public IList<SampleItem> GetAccessibilityItemsWithResource(AccessibilityTestSearch parms)
        {
            var items = context.SampleItems
                .Where(t => t.AccessibilityResourceGroups
                .Any(ar => ar.AccessibilityResources
                .Any(ac => (parms.Resource.Contains(ac.Label) && ac.Disabled == parms.State)))).Take(10).ToList();
            
            var itemKeys = items.Select(i => i.ToItemCardViewModel().ItemKey).ToList();
            return items;
        }

        public IList<BriefSampleItem> GetItemsWithClaim(string claim)
        {
            var items = context.SampleItems.Where(sitem => sitem.Claim.Label == claim).ToList();
            var briefItems = items.Select(i => BriefSampleItem.FromSampleItem(i)).ToList();
            return briefItems;
        }

        public IList<BriefSampleItem> GetAccessibilityTestItems(string domainUrl = "") // Return a list of the top 10 items suitable for testing
        {
            int numAcceptableDisables = 5;
            var baseUrl = domainUrl;
            var rand = new Random();
            var briefElementaryItems = context.SampleItems
                .Where(item => item.Grade <= GradeLevels.Elementary)
                .Select(item => BriefSampleItem.FromSampleItem(item, baseUrl)).ToImmutableArray();
            var briefHighItems = context.SampleItems
                .Where(item => (item.Grade <= GradeLevels.High && item.Grade > GradeLevels.Middle))
                .Select(item => BriefSampleItem.FromSampleItem(item, baseUrl)).ToImmutableArray();

            // var numDisabledEelementaryItems = briefElementaryItems.Where(item => item.AccessibilityResources
            //     .Any(res => res.Disabled == true)).ToImmutableArray();
            // var numDisabledHighItems = briefHighItems.Where(item => item.AccessibilityResources
            //     .Any(res => res.Disabled == true)).ToImmutableArray();
            
            var lowTestableItems = briefElementaryItems.Where(item => 
                item.AccessibilityResources.Where(res => res.Disabled == true).Count()
                <= numAcceptableDisables).ToImmutableArray();

            var highTestableItems = briefHighItems.Where(item => 
                item.AccessibilityResources.Where(res => res.Disabled == true).Count() 
                <= numAcceptableDisables).ToImmutableArray();

            var lowTestKey = lowTestableItems.ElementAt(rand.Next(lowTestableItems.Count()));
            var highTestKey = highTestableItems.ElementAt(rand.Next(highTestableItems.Count()));

            List<BriefSampleItem> lowTestItems = new List<BriefSampleItem>();
            List<BriefSampleItem> highTestItems = new List<BriefSampleItem>();

            lowTestItems.Add(lowTestKey);
            highTestItems.Add(highTestKey);

            foreach(AccessibilityResource lowResource in lowTestKey.AccessibilityResources.Where(res => res.Disabled == true))
            {
                var checkForResource = lowTestItems
                    .Any(item => item.AccessibilityResources
                    .Any(res => res.Label == lowResource.Label && res.Disabled == false));
                if (!checkForResource)
                {
                    var itemsUsingDisabledResource = briefElementaryItems
                        .Where(item => item.AccessibilityResources
                        .Any(res => res.Label == lowResource.Label && res.Disabled == false)).ToList();
                    try
                    {
                        lowTestItems.Add(itemsUsingDisabledResource
                            .ElementAt(rand.Next(itemsUsingDisabledResource.Count())));
                    }
                    catch (System.ArgumentOutOfRangeException e)
                    {
                        Console.WriteLine("No Elementary Level Items found with {0} Resource Enabled", lowResource.Label);
                        Console.WriteLine("Exception source: {0}", e.Source);
                    }
                }
            }

            foreach(AccessibilityResource highResource in highTestKey.AccessibilityResources.Where(res => res.Disabled == true))
            {
                var checkForResource = highTestItems
                    .Any(item => item.AccessibilityResources
                    .Any(res => res.Label == highResource.Label && res.Disabled == false));
                if (!checkForResource)
                {
                    var itemsUsingDisabledResource = briefHighItems
                        .Where(item => item.AccessibilityResources
                        .Any(res => res.Label == highResource.Label && res.Disabled == false)).ToList();
                    try
                    {
                        highTestItems.Add(itemsUsingDisabledResource
                            .ElementAt(rand.Next(itemsUsingDisabledResource.Count())));
                    }
                    catch (System.ArgumentOutOfRangeException e)
                    {
                        Console.WriteLine("No High School Level Items found with {0} Resource Enabled", highResource.Label);
                        Console.WriteLine("Exception Source: {0}", e.Source);
                    }
                }
            }

            var testSet = lowTestItems.Concat(highTestItems).ToList();
            return testSet;

        }

        public IList<AccessibilityTestItem> FormatTestItems(IList<BriefSampleItem> testItems)
        {
            var rand = new Random();
            SampleItem resourceListItem = context.SampleItems.First();
            ImmutableArray<AccessibilityResource> resourceList = resourceListItem.AccessibilityResourceGroups
                .SelectMany(group => group.AccessibilityResources.Select(r => r)).ToImmutableArray();
            List<string> accessibilityResourceTitles = resourceList.Select(r => r.Label).ToList();
            List<AccessibilityTestItem> itemsUnderTest = new List<AccessibilityTestItem>();

            foreach(string selectedResource in accessibilityResourceTitles)
            {
                var selectedItem = testItems.ElementAt(rand.Next(testItems.Count()));
                while (selectedItem.AccessibilityResources.Any(res => res.Label == selectedResource && res.Disabled == true))
                    selectedItem = testItems.ElementAt(rand.Next(testItems.Count()));
                itemsUnderTest.Add(AccessibilityTestItem.FromBriefSampleItem(selectedItem, selectedResource, context));
            }
            return itemsUnderTest;
        }
    }
}
