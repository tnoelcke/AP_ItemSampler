﻿using Microsoft.Extensions.Options;
using SmarterBalanced.SampleItems.Dal.Models;
using SmarterBalanced.SampleItems.Dal.Models.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmarterBalanced.SampleItems.Dal.Interfaces
{
    public interface ISampleItemsRepo
    {
        /// <summary>
        /// Get all ItemDigests with default order (BankKey, then ItemKey).
        /// </summary>
        /// <returns>
        /// An IEnumerable of ItemDigests
        /// </returns>
        IEnumerable<ItemDigest> GetItemDigests();

        /// <summary>
        /// Adds the appsettings configuration into AppSettings class
        /// </summary>
        /// <param name="configurations"></param>
        /// TODO: Throw custom exception and add error logging
        AppSettings GetSettings();

        /// <summary>
        /// Get all ItemDigests matching the given predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>An IEnumerable of ItemDigests</returns>
        IEnumerable<ItemDigest> GetItemDigests(Func<ItemDigest, bool> predicate);

        /// <summary>
        /// Retreives the single specified ItemDigest.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>ItemDigest</returns>
        ItemDigest GetItemDigest(Func<ItemDigest, bool> predicate);

        /// <summary>
        /// Get ItemDigest matching the specified identifier keys
        /// </summary>
        /// <param name="bankKey"></param>
        /// <param name="itemKey"></param>
        /// <returns>ItemDigest</returns>
        ItemDigest GetItemDigest(int bankKey, int itemKey);
    }
}
