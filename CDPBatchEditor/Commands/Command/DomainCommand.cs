//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DomainComand.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Commands.Command
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Defines an <see cref="DomainCommand" /> that provides actions that are
    /// <see cref="CDP4Common.SiteDirectoryData.DomainOfExpertise" /> related
    /// </summary>
    public class DomainCommand : IDomainCommand
    {
        /// <summary>
        /// Gets the injected <see cref="ICommandArguments" /> instance
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// Gets the injected <see cref="IFilterService" /> instance
        /// </summary>
        private readonly IFilterService filterService;

        /// <summary>
        /// Gets the injected <see cref="ISessionService" /> instance
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Initialise a new <see cref="DomainCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public DomainCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Changes ownership owned items from one <see cref="CDP4Common.SiteDirectoryData.DomainOfExpertise" /> to another.
        /// </summary>
        public void ChangeDomain()
        {
            if (this.commandArguments.DomainOfExpertise == this.commandArguments.ToDomainOfExpertise)
            {
                Console.WriteLine($"The from and to domains are the same. No changes performed.");
                return;
            }

            var fromDomain = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == this.commandArguments.DomainOfExpertise);

            if (fromDomain == null)
            {
                Console.WriteLine($"The from-domain {this.commandArguments.DomainOfExpertise} cannot be found.");
                return;
            }

            var toDomain = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == this.commandArguments.ToDomainOfExpertise);

            if (toDomain == null)
            {
                Console.WriteLine($"The to-domain {this.commandArguments.ToDomainOfExpertise} cannot be found.");
                return;
            }

            foreach (var elementDefinition in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e)))
            {
                var elementDefinitionOwner = elementDefinition.Owner;

                if (elementDefinitionOwner == fromDomain)
                {
                    this.MakeTheChangeOfOwnerOnThing(elementDefinition.Clone(true), toDomain);
                    Console.WriteLine($"Changed owner of {elementDefinition.ShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                }

                foreach (var parameter in elementDefinition.Parameter.Where(p => this.filterService.IsParameterSpecifiedOrAny(p)))
                {
                    if (parameter.Owner == fromDomain)
                    {
                        this.MakeTheChangeOfOwnerOnThing(parameter.Clone(true), toDomain);
                        Console.WriteLine($"Changed owner of {parameter.UserFriendlyShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                    }

                    foreach (var subscription in parameter.ParameterSubscription.Where(subscription => subscription.Owner == fromDomain))
                    {
                        this.MakeTheChangeOfOwnerOnThing(subscription.Clone(true), toDomain);
                        Console.WriteLine($"Changed owner of {subscription.UserFriendlyShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                    }

                    foreach (var elementUsage in elementDefinition.ContainedElement)
                    {
                        if (elementUsage.Owner == fromDomain)
                        {
                            this.MakeTheChangeOfOwnerOnThing(elementUsage.Clone(true), toDomain);
                            Console.WriteLine($"Changed owner of {elementUsage.ShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                        }

                        foreach (var parameterOverride in elementUsage.ParameterOverride)
                        {
                            if (parameterOverride.Owner == fromDomain)
                            {
                                this.MakeTheChangeOfOwnerOnThing(parameterOverride.Clone(true), toDomain);
                                Console.WriteLine($"Changed owner of {parameterOverride.UserFriendlyShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                            }

                            foreach (var subscription in parameterOverride.ParameterSubscription.Where(subscription => subscription.Owner == fromDomain))
                            {
                                this.MakeTheChangeOfOwnerOnThing(subscription.Clone(true), toDomain);
                                Console.WriteLine($"Changed owner of {subscription.UserFriendlyShortName} from {fromDomain.ShortName} to {toDomain.ShortName}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the ownership of all parameters inside element definitions that have a name starting with "Generic Equipment".
        /// </summary>
        public void SetGenericEquipmentOwnership()
        {
            var pwrDomain = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == "PWR");
            var sysDomain = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == "SYS");
            var confDomain = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == "CONF");

            var prescribedOwnership = new Dictionary<string, DomainOfExpertise>
            {
                ["P_mean"] = pwrDomain,
                ["P_duty_cyc"] = sysDomain,
                ["loc"] = confDomain
            };

            foreach (var elementDefinition in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e)))
            {
                var elementDefinitionOwner = elementDefinition.Owner;

                if (elementDefinition.Name.StartsWith("Generic Equipment"))
                {
                    foreach (var parameter in elementDefinition.Parameter)
                    {
                        if (prescribedOwnership.TryGetValue(parameter.ParameterType.ShortName, out var newOwner))
                        {
                            // Change ownership only if different from what is prescribed
                            if (parameter.Owner == newOwner)
                            {
                                newOwner = null;
                            }
                        }
                        else if (parameter.Owner != elementDefinitionOwner)
                        {
                            newOwner = elementDefinitionOwner;
                        }

                        // Change ownership if needed
                        if (newOwner != null)
                        {
                            this.MakeTheChangeOfOwnerOnThing(parameter.Clone(true), newOwner);
                            Console.WriteLine($"Changed owner of {parameter.UserFriendlyShortName} from {parameter.Owner.ShortName} to {newOwner.ShortName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the ownership of all parameters provided to the provided owner
        /// </summary>
        public void ChangeParameterOwnership()
        {
            var newOwner = this.sessionService.SiteDirectory.Domain.SingleOrDefault(d => d.ShortName == this.commandArguments.DomainOfExpertise);

            if (newOwner == null)
            {
                Console.WriteLine($"Cannot find domain of expertise for {this.commandArguments.DomainOfExpertise}");
            }
            else
            {
                foreach (var elementDefinition in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e)))
                {
                    var elementDefinitionOwner = elementDefinition.Owner;

                    foreach (var parameter in elementDefinition.Parameter
                        .Where(
                            p => this.commandArguments.SelectedParameters.Contains(p.ParameterType.ShortName)
                                 && p.Owner != newOwner))
                    {
                        this.MakeTheChangeOfOwnerOnThing(parameter.Clone(false), newOwner);
                        Console.WriteLine($"Changed owner of {parameter.UserFriendlyShortName} from {parameter.Owner.ShortName} to {newOwner.ShortName}");
                    }
                }
            }
        }

        /// <summary>
        /// Create the transaction and update the owner
        /// </summary>
        /// <param name="thingClone">the <see cref="Thing" /> cloned to make the change of owner on</param>
        /// <param name="newOwner">the <see cref="DomainOfExpertise" /> to set as new owner of the <see cref="Thing" /> cloned</param>
        private void MakeTheChangeOfOwnerOnThing(Thing thingClone, DomainOfExpertise newOwner)
        {
            this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(thingClone), thingClone));
            ((IOwnedThing) thingClone).Owner = newOwner;
            this.sessionService.Transactions.Last().CreateOrUpdate(thingClone);
        }
    }
}
