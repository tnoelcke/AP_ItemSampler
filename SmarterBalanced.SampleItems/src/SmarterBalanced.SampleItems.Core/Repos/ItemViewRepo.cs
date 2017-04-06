﻿using SmarterBalanced.SampleItems.Core.Repos.Models;
using SmarterBalanced.SampleItems.Dal.Providers;
using SmarterBalanced.SampleItems.Dal.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SmarterBalanced.SampleItems.Core.Translations;
using SmarterBalanced.SampleItems.Dal.Configurations.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Immutable;
using CoreFtp;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace SmarterBalanced.SampleItems.Core.Repos
{
    public class ItemViewRepo : IItemViewRepo
    {
        private readonly SampleItemsContext context;
        private readonly ILogger logger;

        public ItemViewRepo(SampleItemsContext context, ILoggerFactory loggerFactory)
        {
            this.context = context;
            logger = loggerFactory.CreateLogger<ItemViewRepo>();
        }

        public SampleItem GetSampleItem(int bankKey, int itemKey)
        {
            return context.SampleItems.SingleOrDefault(item => item.BankKey == bankKey && item.ItemKey == itemKey);
        }

        public ItemCardViewModel GetItemCardViewModel(int bankKey, int itemKey)
        {
            return context.ItemCards.SingleOrDefault(item => item.BankKey == bankKey && item.ItemKey == itemKey);
        }

        /// <summary>
        /// Constructs an itemviewerservice URL to access the 
        /// item corresponding to the given SampleItem.
        /// </summary>
        public string GetItemViewerUrl(SampleItem item)
        {
            string items;
            string baseUrl = context.AppSettings.SettingsConfig.ItemViewerServiceURL;
            if (item == null)
            {
                return string.Empty;
            }

            if (item.IsPerformanceItem)
            {
                items = string.Join(",", GetPeformanceItemNames(item));
            }
            else
            {
                items = item.ToString();
            }

            return $"{baseUrl}/items?ids={items}";
        }

        /// <summary>
        /// Gets a list of items that share a stimulus with the given item.
        /// Given item is returned as the first element of the list.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private List<SampleItem> GetAssociatedPerformanceItems(SampleItem item)
        {
            var associatedStimulus = item.AssociatedStimulus;
            List<SampleItem> associatedStimulusDigests = context.SampleItems
                .Where(i => i.IsPerformanceItem &&
                    i.AssociatedStimulus == item.AssociatedStimulus &&
                    (i.FieldTestUse != null 
                        && i.FieldTestUse.Code.Equals(item.FieldTestUse?.Code))
                    )
                .OrderByDescending(i => i.ItemKey == item.ItemKey)
                .ThenBy(i => i.FieldTestUse?.Section)
                .ThenBy(i => i.FieldTestUse?.QuestionNumber).ToList();

            return associatedStimulusDigests;
        }

        private List<string> GetPeformanceItemNames(SampleItem item)
        {
            var associatedStimulusDigests = GetAssociatedPerformanceItems(item)?.Select(d => d.ToString()).ToList();
            return associatedStimulusDigests;
        }

        private string GetPerformanceDescription(SampleItem item)
        {
            if(!item.IsPerformanceItem)
            {
                //No description for non performance items
                return string.Empty;
            }

            if(item.Subject.Code.ToLower() == "math")
            {
                return context.AppSettings.SettingsConfig.MATHPerformanceDescription;
            }

            else if (item.Subject.Code.ToLower() == "ela")
            {
                return context.AppSettings.SettingsConfig.ELAPerformanceDescription;
            }
            //Unknown subject
            return string.Empty;
        }

        private ImmutableArray<string> GetItemBrailleCodes(SampleItem item)
        {
            List<string> brailleCodes = new List<string>();
            foreach(AccessibilityResourceGroup group in item.AccessibilityResourceGroups)
            {
                foreach (AccessibilityResource res in group.AccessibilityResources)
                {

                    if(res.ResourceCode == "BrailleType")
                    {
                        foreach (AccessibilitySelection sel in res.Selections)
                        {
                            brailleCodes.Add(sel.SelectionCode);
                        }
                    }

                }
            }
            return brailleCodes.ToImmutableArray();
        }

        private string getBrailleTypeFromCode(string code)
        {
            //Codes look like TDS_BT_TYPE except for the no braille code which looks like TDS_BT0
            var bt = code.Split('_');
            if(bt.Length != 3)
            {
                return string.Empty;
            }
            return bt[2];  
        }

        /// <summary>
        /// Takes an item and a list of braille accessibility codes and returns which ones have files associated with them on the SB FTP server.
        /// </summary>
        /// <param name="codes"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private async Task<ImmutableArray<string>> AvailableBrailleFormats(IEnumerable<string> codes, SampleItem item)
        {
            BlockingCollection<string> validCodes = new BlockingCollection<string>();
            var subject = item.Subject.Code;
            var grade = item.Grade.IndividualGradeToNumString();
            var itemId = item.ItemKey;

            var ftpClient = new FtpClient(new FtpClientConfiguration
            {
                Host = "ftps.smarterbalanced.org",
                Username = "anonymous",
                Password = "guest"
            });
            await ftpClient.LoginAsync();
            foreach(string code in codes)
            {
                var brailleType = getBrailleTypeFromCode(code);
                if(brailleType == "")
                {
                    //If there is no braille type to not check for a file
                    continue;
                }
                string filePath = BuildBrailleFilePath(item, brailleType);
                try {
                    var filesize = await ftpClient.GetFileSizeAsync(filePath);
                    validCodes.Add(code);
                } catch(Exception)
                {
                    //No file for that combination of itemKey, grade, subject, and braille type
                }
            }
            return validCodes.ToImmutableArray();
        }

        private string BuildBrailleFilePath(SampleItem item, string code)
        {
            var subject = item.Subject.Code;
            var grade = item.Grade.IndividualGradeToNumString();
            var itemId = item.ItemKey;
            return $"~sbacpublic/Public/PracticeAndTrainingTests/2016-2017_PracticeAndTrainingBrailleFiles/{subject}/{grade}/item-{itemId}/item_{itemId}_enu_{code}.brf";
        }

        public async Task<Stream> GetFtpFile(int itemBank, int itemKey, string brailleCode)
        {
            SampleItem item = GetSampleItem(itemBank, itemKey);
            var brailleType = getBrailleTypeFromCode(brailleCode);
            if(brailleType == string.Empty)
            {
                throw new ArgumentException("Invalid Braille Type");
            }
            var ftpClient = new FtpClient(new FtpClientConfiguration
            {
                Host = "ftps.smarterbalanced.org",
                Username = "anonymous",
                Password = "guest"
            });
            await ftpClient.LoginAsync();
            var filePath = BuildBrailleFilePath(item, brailleType);
            var ftpStream = await ftpClient.OpenFileReadStreamAsync(filePath);
            return ftpStream;
        }

        public async Task<ItemViewModel> GetItemViewModel(
            int bankKey,
            int itemKey,
            string[] iSAAPCodes,
            Dictionary<string, string> cookiePreferences)
        {
            var sampleItem = GetSampleItem(bankKey, itemKey);
            if (sampleItem == null)
            {
                return null;
            }

            var aboutThisItem = GetAboutThisItemViewModel(sampleItem);

            var groups = sampleItem.AccessibilityResourceGroups.ApplyPreferences(iSAAPCodes, cookiePreferences);

            //Checking for file exsistence is slooooooooooow.
            //We should check if the item supports braille first
            var brailleItemCodes = await AvailableBrailleFormats(GetItemBrailleCodes(sampleItem), sampleItem);
            var braillePassageCodes = await AvailableBrailleFormats(GetItemBrailleCodes(sampleItem), sampleItem);

            var itemViewModel = new ItemViewModel(
                itemViewerServiceUrl: GetItemViewerUrl(sampleItem),
                accessibilityCookieName: context.AppSettings.SettingsConfig.AccessibilityCookie,
                isPerformanceItem: sampleItem.IsPerformanceItem,
                accResourceGroups: groups,
                moreLikeThisVM: GetMoreLikeThis(sampleItem),
                aboutThisItemVM: aboutThisItem,
                subject: sampleItem.Subject.Code,
                brailleItemCodes: brailleItemCodes,
                braillePassageCodes: braillePassageCodes,
                performanceItemDescription: GetPerformanceDescription(sampleItem));

            return itemViewModel;
        }

        public AboutThisItemViewModel GetAboutThisItemViewModel(SampleItem sampleItem)
        {
            if (sampleItem == null)
            {
                return null;
            }

            var itemCardViewModel = GetItemCardViewModel(sampleItem.BankKey, sampleItem.ItemKey);
            var aboutThisItemViewModel = new AboutThisItemViewModel(
                rubrics: sampleItem.Rubrics,
                itemCard: itemCardViewModel,
                targetDescription: sampleItem.CoreStandards?.TargetDescription,
                depthOfKnowledge: sampleItem.DepthOfKnowledge,
                commonCoreStandardsDescription: sampleItem.CoreStandards?.CommonCoreStandardsDescription);

            return aboutThisItemViewModel;
        }

        private MoreLikeThisColumn ToColumn(IEnumerable<ItemCardViewModel> itemCards, GradeLevels grade)
        {
            if (itemCards == null)
            {
                return null;
            } 

            string label = grade.ToDisplayString();
            var column = new MoreLikeThisColumn(
                label: label, itemCards: itemCards.ToImmutableArray());

            return column;
        }

        /// <summary>
        /// Gets up to 3 items same grade, grade above, and grade below. All items 
        /// </summary>
        /// <param name="grade"></param>
        /// <param name="subject"></param>
        /// <param name="claim"></param>
        public MoreLikeThisViewModel GetMoreLikeThis(SampleItem sampleItem)
        {
            var subjectCode = sampleItem.Subject.Code;
            var claimCode = sampleItem.Claim?.Code;
            var grade = sampleItem.Grade;
            var itemKey = sampleItem.ItemKey;
            var bankKey = sampleItem.BankKey;

            var matchingSubjectClaim = context.ItemCards.Where(i => i.SubjectCode == subjectCode && i.ClaimCode == claimCode);
            int numExpected = context.AppSettings.SettingsConfig.NumMoreLikeThisItems;

            var comparer = new MoreLikeThisComparer(subjectCode, claimCode);

            bool isHighSchool = GradeLevels.High.Contains(grade);
            GradeLevels gradeBelow = isHighSchool ? GradeLevels.Grade8 : grade.GradeBelow();
            GradeLevels gradeAbove = grade.GradeAbove();

            IEnumerable<ItemCardViewModel> cardsGradeAbove = null;

            // Only display above if not a high school grade
            if (!isHighSchool)
            {
                // take grade 11 if gradeabove is high school (only high school items are grade 11)
                gradeAbove = GradeLevels.High.Contains(gradeAbove) ? GradeLevels.Grade11 : gradeAbove;
                cardsGradeAbove = context.ItemCards
                    .Where(i => i.Grade == gradeAbove)
                    .OrderBy(i => i, comparer)
                    .Take(numExpected);
            }

            var cardsSameGrade = context.ItemCards
                .Where(i => i.Grade == grade && i.ItemKey != itemKey)
                .OrderBy(i => i, comparer)
                .Take(numExpected);

            var cardsGradeBelow = context.ItemCards
                .Where(i => i.Grade == gradeBelow)
                .OrderBy(i => i, comparer)
                .Take(numExpected);
            
            var moreLikeThisVM = new MoreLikeThisViewModel(
                ToColumn(cardsGradeBelow, gradeBelow),
                ToColumn(cardsSameGrade, grade),
                ToColumn(cardsGradeAbove, gradeAbove)
                );

            return moreLikeThisVM;
        }

    }

}
