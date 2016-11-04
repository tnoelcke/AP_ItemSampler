﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmarterBalanced.SampleItems.Core.Repos.Models
{
    public class LocalAccessibilityViewModel
    {
        public List<AccessibilityResourceViewModel> AccessibilityResourceViewModels { get; set; }

        public string NonApplicableAccessibilityResources { get; set; }
    }
}
