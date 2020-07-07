//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SessionService.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.DAL;
    using CDP4Dal.Operations;

    using CDP4ServicesDal;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Extensions;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Session service provides a <see cref="CDP4Dal.ISession" />
    /// </summary>
    public class SessionService : ISessionService
    {
        /// <summary>
        /// Gets the injected commandArguments
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// Gets the <see cref="IFilterService" /> injected instance
        /// </summary>
        private readonly IFilterService filterService;

        /// <summary>
        /// Holds the credentials used to connect to the server
        /// </summary>
        private Credentials credentials;

        /// <summary>
        /// Empty constructor used for test purpose
        /// </summary>
        public SessionService(ICommandArguments commandArguments, IFilterService filterService, ISession session)
        {
            this.commandArguments = commandArguments;
            this.filterService = filterService;
            this.Session = session;
        }

        /// <summary>
        /// Initialise a new <see cref="SessionService" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance.</param>
        /// <param name="filterService">the <see cref="IFilterService" /> that provides filters and filters helpers.</param>
        [ExcludeFromCodeCoverage]
        public SessionService(ICommandArguments commandArguments, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.filterService = filterService;

            if (!this.IsSessionOpen())
            {
                this.Open();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ISession" />
        /// </summary>
        public ISession Session { get; private set; }

        /// <summary>
        /// Gets or sets the selected <see cref="Iteration" />
        /// </summary>
        public Iteration Iteration { get; private set; }

        /// <summary>
        /// Gets the cache of the current <see cref="ISession" />
        /// </summary>
        [ExcludeFromCodeCoverage]
        public ConcurrentDictionary<CacheKey, Lazy<Thing>> Cache
        {
            get { return this.Session.Assembler.Cache; }
        }

        /// <summary>
        /// Holds all the <see cref="ThingTransaction" /> to apply to the database
        /// </summary>
        public List<ThingTransaction> Transactions { get; } = new List<ThingTransaction>();

        /// <summary>
        /// Gets or sets the domain of expertise
        /// </summary>
        public DomainOfExpertise DomainOfExpertise { get; private set; }

        /// <summary>
        /// Gets or sets the current <see cref="CDP4Common.SiteDirectoryData.SiteDirectory" />
        /// </summary>
        public SiteDirectory SiteDirectory { get; private set; }

        /// <summary>
        /// Opens a session and set the properties
        /// </summary>
        public void Open()
        {
            var dal = new CdpServicesDal();

            this.credentials = new Credentials(this.commandArguments.UserName, this.commandArguments.Password, this.commandArguments.ServerUri);
            this.Session ??= new Session(dal, this.credentials);
            this.Session.Open().GetAwaiter().GetResult();

            if (this.SetProperties())
            {
                this.filterService.ProcessFilters(this.Iteration, this.SiteDirectory.Domain);
            }
        }

        /// <summary>
        /// Checks whether the session is open by checking if the <see cref="CDP4Common.SiteDirectoryData.SiteDirectory" /> is
        /// available
        /// </summary>
        public bool IsSessionOpen()
        {
            return this.Session?.RetrieveSiteDirectory() != null;
        }

        /// <summary>
        /// Queries the selected <see cref="T:CDP4Common.SiteDirectoryData.DomainOfExpertise" /> from the session for provided
        /// current <see cref="T:CDP4Common.EngineeringModelData.Iteration" />
        /// </summary>
        /// <returns>
        /// A <see cref="T:CDP4Common.SiteDirectoryData.DomainOfExpertise" /> if has been selected for the
        /// <see cref="T:CDP4Common.EngineeringModelData.Iteration" />, null otherwise.
        /// </returns>
        [ExcludeFromCodeCoverage]
        public DomainOfExpertise QuerySelectedDomainOfExpertise()
        {
            return this.Session.QuerySelectedDomainOfExpertise(this.Iteration);
        }

        /// <summary>
        /// Commits all change to the model
        /// </summary>
        public void Save()
        {
            if (!this.commandArguments.DryRun && this.Transactions.Any())
            {
                Console.Write($"Persisting ({this.Transactions.Count}) changes in progress: <");
                this.Session.Write(this.Transactions).GetAwaiter().GetResult();
                Console.Write($"> done!\n");
            }
            else if (!this.Transactions.Any())
            {
                Console.WriteLine("No change to persist");
            }
            else
            {
                Console.WriteLine("DryRun option is set, changes have not been saved");
            }
        }

        /// <summary>
        /// Wraps the saving of the transaction and the close of the session
        /// </summary>
        public void CloseAndSave()
        {
            this.Save();
            this.Close();
        }

        /// <summary>
        /// Closes connection to the data-source and end the execution of this app
        /// </summary>
        public void Close()
        {
            if (!this.IsSessionOpen())
            {
                Console.WriteLine("At first a connection should be opened.");
                return;
            }

            try
            {
                this.Session.Close().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("During close operation an error is received: ");
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Session has been closed!");
        }

        /// <summary>
        /// Retrieves <see cref="CDP4Common.EngineeringModelData.Iteration" />,
        /// <see cref="CDP4Common.SiteDirectoryData.DomainOfExpertise" />, <see cref="EngineeringModel" />
        /// And process filter <see cref="IFilterService.ProcessFilters" />
        /// </summary>
        /// <returns>Assert whether the properties are all set and the session is ready to be use</returns>
        public bool SetProperties()
        {
            this.SiteDirectory = this.Session.RetrieveSiteDirectory();

            if (!this.IsSessionOpen())
            {
                Console.WriteLine("At first a connection should be opened.");
                return false;
            }

            var engineeringModelSetup = string.IsNullOrWhiteSpace(this.commandArguments.EngineeringModel) ? this.SiteDirectory.Model[0] : this.SiteDirectory.Model.SingleOrDefault(s => s.ShortName == this.commandArguments.EngineeringModel);

            if (engineeringModelSetup == null)
            {
                Console.WriteLine($"No Engineering Model found with this name {this.commandArguments.EngineeringModel}");
                return false;
            }

            var engineeringModelIid = engineeringModelSetup.EngineeringModelIid;
            var iterationIid = string.IsNullOrWhiteSpace(this.commandArguments.EngineeringModel) ? engineeringModelSetup.IterationSetup[0].IterationIid : engineeringModelSetup.IterationSetup.Single(s => s.FrozenOn == null).IterationIid;
            var domainOfExpertiseIid = string.IsNullOrWhiteSpace(this.commandArguments.DomainOfExpertise) ? engineeringModelSetup.ActiveDomain[0].Iid : engineeringModelSetup.ActiveDomain.Single(s => s.UserFriendlyShortName == this.commandArguments.DomainOfExpertise).Iid;

            var model = new EngineeringModel(engineeringModelIid, this.Session.Assembler.Cache, this.commandArguments.ServerUri);
            var iteration = new Iteration(iterationIid, this.Session.Assembler.Cache, this.commandArguments.ServerUri) { Container = model };
            this.DomainOfExpertise = new DomainOfExpertise(domainOfExpertiseIid, this.Session.Assembler.Cache, this.commandArguments.ServerUri);

            this.Session.Read(iteration, this.DomainOfExpertise).GetAwaiter().GetResult();
            this.Iteration = this.Session.OpenIterations.Keys.First();

            return this.Iteration != null && this.DomainOfExpertise != null;
        }
    }
}
