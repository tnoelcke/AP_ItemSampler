using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using SmarterBalanced.SampleItems.Dal.Providers.Models;

namespace SmarterBalanced.SampleItems.Core.AccessibilityTesting
{
    public class BriefSampleItem
    {
        public int ItemKey;
        public GradeLevels Grade;
        public string SubjectCode;
        public string ClaimLabel;
        public string InteractionTypeLabel;
        public ImmutableArray<BriefAccessibilityResource> BriefResources;

        public BriefSampleItem(
            int itemKey,
            GradeLevels grade,
            string subjectCode,
            string claimLabel,
            string interactionTypeLabel,
            ImmutableArray<BriefAccessibilityResource> briefResources)
        {
            ItemKey = itemKey;
            Grade = grade;
            SubjectCode = subjectCode;
            ClaimLabel = claimLabel;
            InteractionTypeLabel = interactionTypeLabel;
            BriefResources = briefResources;
        }

        public static BriefSampleItem FromSampleItem(SampleItem sampleItem)
        {
            var disabledResources = sampleItem.AccessibilityResourceGroups
                .SelectMany(group => group.AccessibilityResources
                .Where(r => r.Disabled == true).ToImmutableArray()).ToImmutableArray();
            var resourceArray = disabledResources.Select(r => r.ToBriefAccessibilityResource()).ToImmutableArray();
            return new BriefSampleItem(
                itemKey: sampleItem.ItemKey,
                grade: sampleItem.Grade,
                subjectCode: sampleItem.Subject.Label,
                claimLabel: sampleItem.Claim.Label,
                interactionTypeLabel: sampleItem.InteractionType.Label,
                briefResources: resourceArray);
        }
    }
}