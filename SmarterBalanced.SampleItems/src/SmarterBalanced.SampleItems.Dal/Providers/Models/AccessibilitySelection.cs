﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SmarterBalanced.SampleItems.Dal.Providers.Models
{
    public class AccessibilitySelection
    {
        public string SelectionCode { get; }
        public string Label { get; }
        public int? Order { get; }
        public bool Disabled { get; }
        public bool Hidden { get; }

        public AccessibilitySelection(
            string code,
            string label,
            int? order,
            bool disabled,
            bool hidden)
        {
            SelectionCode = code;
            Label = label;
            Order = order;
            Disabled = disabled;
            Hidden = hidden;
        }

        public static AccessibilitySelection Create(
            string code = "",
            string label = "",
            int? order = null,
            bool disabled = false,
            bool hidden = false)
        {
            var sel = new AccessibilitySelection(
                code: code,
                order: order,
                disabled: disabled,
                label: label,
                hidden: hidden);

            return sel;
        }
        
        public AccessibilitySelection WithDisabled(bool disabled)
        {
            var newSelection = new AccessibilitySelection(
                code: SelectionCode,
                label: Label,
                order: Order,
                disabled: disabled,
                hidden: Hidden);

            return newSelection;
        }

        public static AccessibilitySelection Create(XElement xmlSelection)
        {
            var selection = new AccessibilitySelection(
                    code: (string)xmlSelection.Element("Code"),
                    label: (string)xmlSelection.Element("Text")?.Element("Label"),
                    order: (int?)xmlSelection.Element("Order"),
                    disabled: false,
                    hidden: false);

            return selection;
        }
    }

}
