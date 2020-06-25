//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ISessionService.cs" company="RHEA System S.A.">
//     Copyright (c) 2015-2020 RHEA System S.A.
// 
//     Author: Nathanael Smiechowski, Alex Vorobiev, Alexander van Delft, Kamil Wojnowski, Sam Gerené
// 
//     This file is part of CDP4 Batch Editor.
//     The CDP4 Batch Editor is a commandline application to perform batch operations on a
//     ECSS-E-TM-10-25 Annex A and Annex C data source
// 
//     The CDP4 Batch Editor is free software; you can redistribute it and/or
//     modify it under the terms of the GNU Lesser General Public
//     License as published by the Free Software Foundation; either
//     version 3 of the License, or any later version.
// 
//     The CDP4 Batch Editor is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//     GNU Lesser General License version 3 for more details.
// 
//     You should have received a copy of the GNU Lesser General License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace CDPBatchEditor.Services.Interfaces
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    /// <summary>
    /// Defnition of the interface that provides a session service a <see cref="CDP4Dal.ISession" />
    /// </summary>
    public interface ISessionService : IISession
    {
        /// <summary>
        /// Gets the selected <see cref="Iteration" />
        /// </summary>
        Iteration Iteration { get; }

        /// <summary>
        /// Gets or sets the domain of expertise
        /// </summary>
        DomainOfExpertise DomainOfExpertise { get; }

        /// <summary>
        /// Gets or sets the current <see cref="SiteDirectory" />
        /// </summary>
        SiteDirectory SiteDirectory { get; }

        /// <summary>
        /// Gets the cache of the current <see cref="ISession" />
        /// </summary>
        ConcurrentDictionary<CacheKey, Lazy<Thing>> Cache { get; }

        /// <summary>
        /// Holds all the <see cref="ThingTransaction" /> to apply to the database
        /// </summary>
        public List<ThingTransaction> Transactions { get; }

        /// <summary>
        /// Checks whether the session is open by checking if the <see cref="T:CDP4Common.SiteDirectoryData.SiteDirectory" /> is
        /// available
        /// </summary>
        /// <returns>the <see cref="bool" /> value whether the session is actually open</returns>
        bool IsSessionOpen();

        /// <summary>
        /// Queries the selected <see cref="T:CDP4Common.SiteDirectoryData.DomainOfExpertise" /> from the session for provided
        /// current <see cref="T:CDP4Common.EngineeringModelData.Iteration" />
        /// </summary>
        /// <returns>
        /// A <see cref="T:CDP4Common.SiteDirectoryData.DomainOfExpertise" /> if has been selected for the
        /// <see cref="T:CDP4Common.EngineeringModelData.Iteration" />, null otherwise.
        /// </returns>
        DomainOfExpertise QuerySelectedDomainOfExpertise();

        /// <summary>
        /// Closes connection to the data-source and end the execution of this app
        /// </summary>
        void Close();

        /// <summary>
        /// Commits all change to the model
        /// </summary>
        void Save();

        /// <summary>
        /// Wraps the saving of the transaction and the close of the session
        /// </summary>
        void CloseAndSave();

        /// <summary>
        /// Opens a session and set the properties
        /// </summary>
        /// <returns></returns>
        void Open();
    }
}
