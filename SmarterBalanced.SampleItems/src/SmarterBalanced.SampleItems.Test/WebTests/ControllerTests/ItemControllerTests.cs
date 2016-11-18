﻿using Xunit;
using Moq;
using SmarterBalanced.SampleItems.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using SmarterBalanced.SampleItems.Dal.Providers.Models;
using SmarterBalanced.SampleItems.Core.Repos;
using SmarterBalanced.SampleItems.Core.Repos.Models;
using SmarterBalanced.SampleItems.Dal.Configurations.Models;
using System.Collections.Generic;

namespace SmarterBalanced.SampleItems.Test.WebTests.ControllerTests
{
    public class ItemControllerTests
    {
        ItemController controller;
        ItemViewModel itemViewModel;
        ItemViewModel itemViewModelCookie;
        int bankKey;
        int itemKey;
        string iSAAP;

        public ItemControllerTests()
        {
            bankKey = 234343;
            itemKey = 485954;

            var itemDigest = new ItemDigest
            {
                BankKey = bankKey,
                ItemKey = itemKey,
                Grade = GradeLevels.Grade6
            };

            iSAAP = "TDS_test;TDS_test2;";

            string accCookieName = "accessibilitycookie";

            var localAccessibilityViewModel = new LocalAccessibilityViewModel()
            {
                AccessibilityResourceViewModels = new List<AccessibilityResourceViewModel>()
            };

            var appSettings = new AppSettings()
            {
                SettingsConfig = new SettingsConfig()
                {
                    AccessibilityCookie = accCookieName
                }
            };

            itemViewModel = new ItemViewModel()
            {
                ItemDigest = itemDigest,
                ItemViewerServiceUrl = $"http://itemviewerservice.cass.oregonstate.edu/item/{bankKey}-{itemKey}",
                LocalAccessibilityViewModel = localAccessibilityViewModel
               
            };

            itemViewModelCookie = new ItemViewModel()
            {
                LocalAccessibilityViewModel = localAccessibilityViewModel

            };
            var itemViewRepoMock = new Mock<IItemViewRepo>();
          
            itemViewRepoMock.Setup(x => x.GetItemViewModelAsync(bankKey, itemKey)).ReturnsAsync(itemViewModel);
            itemViewRepoMock.Setup(x => x.GetItemViewModelAsync(bankKey, itemKey, iSAAP)).ReturnsAsync(itemViewModel);
            itemViewRepoMock.Setup(x => x.GetItemViewModelAsync(bankKey, itemKey, null)).ReturnsAsync(itemViewModelCookie);
            itemViewRepoMock.Setup(x => x.AppSettings).Returns(appSettings);

            controller = new ItemController(itemViewRepoMock.Object);
        }

        /// <summary>
        /// Tests that an ItemViewModel is returned given a vaid id.
        /// </summary>
        [Fact]
        public async void TestDetailsSuccess()
        {
            var result = await controller.Details(bankKey, itemKey, iSAAP);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ItemViewModel>(viewResult.ViewData.Model);

            Assert.Equal(itemViewModel, model);

        }

        /// <summary>
        /// Tests that a BadRequestResult is returned given a null key
        /// </summary>
        [Fact]
        public async void TestDetailsNullParam()
        {
            var result = await controller.Details(null, itemKey, iSAAP);

            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Tests that a BadRequestResult is returned given a nonexistent key
        /// </summary>
        [Fact]
        public async void TestDetailsBadId()
        {
            var result = await controller.Details(bankKey + 1, itemKey + 1, iSAAP);

            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Tests that a cookie ISSAP is returned instead of param
        /// </summary>
        [Fact]
        public async void TestDetailsNoISAAP()
        {
            var result = await controller.Details(bankKey, itemKey, string.Empty);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ItemViewModel>(viewResult.ViewData.Model);

            Assert.Equal(itemViewModelCookie, model);
        }

        /// <summar>
        /// Tests that a BadRequestResult is returned given a null bank key
        /// </summary>
        [Fact]
        public async void ResetToGlobalAccessibilityNullBankKey()
        {
            var result = await controller.Details(null, itemKey, iSAAP);

            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Tests that a BadRequestResult is returned given a null item  key
        /// </summary>
        [Fact]
        public async void ResetToGlobalAccessibilityNullItemKey()
        {
            var result = await controller.ResetToGlobalAccessibility(bankKey, null);

            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Tests that a BadRequestResult is returned given a bad key
        /// </summary>
        [Fact]
        public async void ResetToGlobalAccessibilityBadKey()
        {
            var result = await controller.ResetToGlobalAccessibility(bankKey + 1, itemKey + 1);

            Assert.IsType<BadRequestResult>(result);
        }

        /// <summary>
        /// Tests that a BadRequestResult is returned given a bad key
        /// </summary>
        [Fact]
        public async void ResetToGlobalAccessibilityHappy()
        {
            var result = await controller.ResetToGlobalAccessibility(bankKey, itemKey);

            var viewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_LocalAccessibility", viewResult.ViewName);

            var model = Assert.IsType<LocalAccessibilityViewModel>(viewResult.ViewData.Model);
            Assert.Equal(itemViewModel.LocalAccessibilityViewModel, model);
        }
    }
}
