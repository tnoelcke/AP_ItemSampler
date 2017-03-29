﻿using SmarterBalanced.SampleItems.Dal.Providers.Models;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SmarterBalanced.SampleItems.Core.Repos.Models
{
    public class ItemViewModel
    {
        public string ItemViewerServiceUrl { get; }

        public string AccessibilityCookieName { get; }

        public bool IsPerformanceItem { get; }

        public AboutThisItemViewModel AboutThisItemVM { get; }

        public ImmutableArray<AccessibilityResourceGroup> AccResourceGroups { get; }

        public MoreLikeThisViewModel MoreLikeThisVM { get; }

        public string PerformanceItemDescription { get; }

        public string Subject { get; }

        public ItemViewModel(
            string itemViewerServiceUrl,
            string accessibilityCookieName,
            bool isPerformanceItem,
            ImmutableArray<AccessibilityResourceGroup> accResourceGroups,
            MoreLikeThisViewModel moreLikeThisVM,
            AboutThisItemViewModel aboutThisItemVM,
            string subject,
            string performanceItemDescription = "")
        {
            ItemViewerServiceUrl = itemViewerServiceUrl;
            AccessibilityCookieName = accessibilityCookieName;
            IsPerformanceItem = isPerformanceItem;
            AboutThisItemVM = aboutThisItemVM;
            AccResourceGroups = accResourceGroups;
            MoreLikeThisVM = moreLikeThisVM;
            Subject = subject;
            PerformanceItemDescription = performanceItemDescription;
        }
    }
}
