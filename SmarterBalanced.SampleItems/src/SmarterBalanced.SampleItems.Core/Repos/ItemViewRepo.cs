﻿using SmarterBalanced.SampleItems.Core.Repos.Models;
using SmarterBalanced.SampleItems.Dal.Providers;
using SmarterBalanced.SampleItems.Dal.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SmarterBalanced.SampleItems.Core.Translations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using CoreFtp;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

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
            if (!item.IsPerformanceItem)
            {
                //No description for non performance items
                return string.Empty;
            }

            if (item.Subject.Code.ToLower() == "math")
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

        private string GetItemRootDirectory(SampleItem item)
        {
            return $"{context.AppSettings.SettingsConfig.BrailleFtpBaseDirectory}{item.Subject.ShortLabel}/{item.Grade.IndividualGradeToNumString()}";
        }
        private string GetItemFtpDirectory(SampleItem item)
        {
           return $"{GetItemRootDirectory(item)}/item-{item.ItemKey}";
        }

        private string GetPassageFtpDirectory(SampleItem item)
        {
            return $"{GetItemRootDirectory(item)}/stim-{item.AssociatedStimulus.Value}";
        }

        private ImmutableArray<string> GetItemBrailleDirectories(SampleItem item)
        {
            List<string> itemDirectories = new List<string>();
            itemDirectories.Add(GetItemFtpDirectory(item));
            if (item.IsPerformanceItem)
            {
                foreach (SampleItem associatedItem in GetAssociatedPerformanceItems(item))
                {
                    itemDirectories.Add(GetItemFtpDirectory(associatedItem));
                }
            }
            if (item.AssociatedStimulus.HasValue)
            {
                itemDirectories.Add(GetPassageFtpDirectory(item));
            }
            return itemDirectories.ToImmutableArray();
        }

        private static string GetBrailleTypeFromCode(string code)
        {
            //Codes look like TDS_BT_TYPE except for the no braille code which looks like TDS_BT0
            var bt = code.Split('_');
            if (bt.Length != 3)
            {
                return string.Empty;
            }
            return bt[2];
        }

        public async Task<Dictionary<string, string>> GetBrailleFileNames(
            FtpClient ftpClient, 
            IEnumerable<string> baseDirectories, 
            string brailleCode)
        {
            string brailleType = GetBrailleTypeFromCode(brailleCode).ToLower();
            Dictionary<string, string> brailleFiles = new Dictionary<string, string>();
            foreach (string directory in baseDirectories)
            {
                
                try
                {
                    await ftpClient.ChangeWorkingDirectoryAsync(directory);
                }
                catch (CoreFtp.Infrastructure.FtpException)
                {
                    continue;
                }
                
                var files = await ftpClient.ListFilesAsync();
                var fileNames = files.Select(f => f.Name).Where(f => Regex.IsMatch(f, $"(?i){brailleType}"));
                foreach(string file in fileNames)
                {
                    if (!brailleFiles.ContainsKey(file))
                    {
                        brailleFiles.Add(file, $"{directory}/{file}");
                    }
                }
            }
            return brailleFiles;
        }

        public static string GenerateBrailleZipName(int itemId, string brailleCode)
        {
            return $"{itemId}-{GetBrailleTypeFromCode(brailleCode)}.zip";
        }


        public async Task<Stream> GetItemBrailleZip(int itemBank, int itemKey, string brailleCode)
        {
            SampleItem item = GetSampleItem(itemBank, itemKey);
            string brailleType = GetBrailleTypeFromCode(brailleCode);
            if (brailleType == string.Empty || item == null)
            {
                throw new ArgumentException("Invalid arguments for item or braille");
            }

            ImmutableArray<string> itemDirectories = GetItemBrailleDirectories(item);
            
            using (var ftpClient = new FtpClient(new FtpClientConfiguration
            {
                Host = context.AppSettings.SettingsConfig.SmarterBalancedFtpHost,
                Username = context.AppSettings.SettingsConfig.SmarterBalancedFtpUsername,
                Password = context.AppSettings.SettingsConfig.SmarterBalancedFtpPassword
            }))
            {
                await ftpClient.LoginAsync();
                var brailleFiles = await GetBrailleFileNames(ftpClient, itemDirectories, brailleCode);

                var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (KeyValuePair<string, string> file in brailleFiles)
                    {
                        var entry = archive.CreateEntry(file.Key);
                        using (var ftpStream = await ftpClient.OpenFileReadStreamAsync(file.Value))
                        using (var entryStream = entry.Open())
                        {
                            ftpStream.CopyTo(entryStream);
                        }
                    }
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }

        }

        public ItemViewModel GetItemViewModel(
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

            var itemViewModel = new ItemViewModel(
                itemViewerServiceUrl: GetItemViewerUrl(sampleItem),
                accessibilityCookieName: context.AppSettings.SettingsConfig.AccessibilityCookie,
                isPerformanceItem: sampleItem.IsPerformanceItem,
                accResourceGroups: groups,
                moreLikeThisVM: GetMoreLikeThis(sampleItem),
                aboutThisItemVM: aboutThisItem,
                subject: sampleItem.Subject.Code,
                brailleItemCodes: sampleItem.BrailleItemCodes,
                braillePassageCodes: sampleItem.BraillePassageCodes,
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
