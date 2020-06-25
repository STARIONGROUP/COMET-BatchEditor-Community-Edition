//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SubscriptionCommand.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Defines an <see cref="SubscriptionCommand" /> that provides actions that are
    /// <see cref="CDP4Common.EngineeringModelData.ParameterSubscription" /> related
    /// </summary>
    public class SubscriptionCommand : ISubscriptionCommand
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
        /// Initialise a new <see cref="SubscriptionCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public SubscriptionCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Subscribe parameters and parameter overrides with given short names for given subscriber.
        /// </summary>
        public void Subscribe()
        {
            var subscriber = this.sessionService.SiteDirectory.Domain.SingleOrDefault(d => d.ShortName == this.commandArguments.DomainOfExpertise);

            if (subscriber == null)
            {
                Console.WriteLine($"Unknown subscriber domain of expertise: \"{this.commandArguments.DomainOfExpertise}\". Subscribe parameters skipped.");
                return;
            }

            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("No --parameters given. Subscribe parameters skipped.");
                return;
            }

            foreach (var elementDefinition in this.sessionService.Iteration.Element
                .Where(e => this.filterService.IsFilteredIn(e))
                .OrderBy(x => x.ShortName))
            {
                // Visit all parameters in the element definitions and take a subscription if requested and not taken already
                foreach (var parameter in elementDefinition.Parameter.OrderBy(x => x.ParameterType.ShortName))
                {
                    // Take subscription on this parameter if no subscription taken already
                    if (!this.commandArguments.SelectedParameters.Contains(parameter.ParameterType.ShortName)
                        || parameter.Owner.Iid == subscriber.Iid
                        || parameter.ParameterSubscription.Any(p => p.Owner == this.sessionService.DomainOfExpertise))
                    {
                        continue;
                    }

                    var parameterClone = parameter.Clone(true);

                    this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone));

                    var parameterSubscription = new ParameterSubscription(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                        { Owner = subscriber };

                    this.sessionService.Transactions.Last().Create(parameterSubscription, parameterClone);

                    Console.WriteLine($"Parameter Subscription taken on {parameterSubscription.UserFriendlyShortName}");
                }

                // Visit all parameter overrides in the element usages and take a subscription if requested and not taken already
                foreach (var elementUsage in elementDefinition.ContainedElement.OrderBy(x => x.ShortName))
                {
                    foreach (var parameterOverride in elementUsage.ParameterOverride.OrderBy(x => x.ParameterType.ShortName))
                    {
                        // Take subscription on this parameter override if no subscription is taken already
                        if (this.commandArguments.SelectedParameters.Contains(parameterOverride.ParameterType.ShortName) && parameterOverride.Owner.Iid != subscriber.Iid
                                                                                                                         && parameterOverride.ParameterSubscription.All(parameterSubscription => parameterSubscription.Owner != subscriber))
                        {
                            var parameterOverrideClone = parameterOverride.Clone(true);
                            this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(parameterOverrideClone), parameterOverrideClone));

                            var parameterSubscription = new ParameterSubscription(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                                { Owner = subscriber, Container = parameterOverrideClone };

                            this.sessionService.Transactions.Last().Create(parameterSubscription, parameterOverrideClone);
                            Console.WriteLine($"Parameter Override Subscription taken on {parameterSubscription.UserFriendlyShortName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the switch on all value sets of the <see cref="ParameterSubscription" />s of the given subscriber to the given
        /// switch value.
        /// </summary>
        public void SetParameterSubscriptionsSwitch()
        {
            var subscriber = this.sessionService.SiteDirectory.Domain.SingleOrDefault(domainOfExpertise => domainOfExpertise.ShortName == this.commandArguments.DomainOfExpertise);

            if (subscriber == null)
            {
                Console.WriteLine($"Unknown subscriber domain of expertise: \"{this.commandArguments.DomainOfExpertise}\". Subscribe parameters skipped.");
                return;
            }

            if (!this.commandArguments.ParameterSwitchKind.HasValue)
            {
                Console.WriteLine(
                    "Parameter switch kind not provided: use \"-parameter-switch\" with one of the following values: " +
                    "\"COMPUTED\" | \"MANUAL\" | \"REFERENCE\" . Subscribe parameters skipped.");

                return;
            }

            var changeCount = 0;

            foreach (var elementDefinition in this.sessionService.Iteration.Element
                .Where(x => this.filterService.IsFilteredIn(x)).OrderBy(x => x.ShortName))
            {
                // Visit all parameters in the element definitions and take a subscription if requested and not taken already
                foreach (var parameter in elementDefinition.Parameter.Where(p => this.filterService.IsParameterSpecifiedOrAny(p)).OrderBy(x => x.ParameterType.ShortName))
                {
                    foreach (var parameterSubscriptionValueSet in parameter.ParameterSubscription
                        .Where(p => p.Owner == subscriber)
                        .SelectMany(p => p.ValueSet))
                    {
                        this.UpdateValueSwitch(parameterSubscriptionValueSet);
                        changeCount++;
                    }
                }

                // Visit all parameter overrides in the element usages and take a subscription if requested and not taken already
                foreach (var elementUsage in elementDefinition.ContainedElement.OrderBy(x => x.ShortName))
                {
                    foreach (var parameterOverride in elementUsage.ParameterOverride.OrderBy(x => x.ParameterType.ShortName))
                    {
                        foreach (var parameterSubscriptionValueSet in parameterOverride.ParameterSubscription
                            .Where(p => p.Owner == subscriber)
                            .SelectMany(p => p.ValueSet))
                        {
                            this.UpdateValueSwitch(parameterSubscriptionValueSet);

                            changeCount++;
                        }
                    }
                }
            }

            Console.WriteLine(
                $"Set switch to {this.commandArguments.ParameterSwitchKind} on {changeCount} Parameter or Parameter Override Subscriptions for {subscriber.ShortName}");
        }

        /// <summary>
        /// Update the value switch and create a transaction to persist the change
        /// </summary>
        /// <param name="parameterSubscriptionValueSet">the value set on to change the value switch</param>
        private void UpdateValueSwitch(ParameterSubscriptionValueSet parameterSubscriptionValueSet)
        {
            var thingClone = parameterSubscriptionValueSet.Clone(true);
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(thingClone), thingClone);
            thingClone.ValueSwitch = this.commandArguments.ParameterSwitchKind.Value;
            transaction.CreateOrUpdate(thingClone);
            this.sessionService.Transactions.Add(transaction);
        }
    }
}
