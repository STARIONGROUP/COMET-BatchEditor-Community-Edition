//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="StateCommand.cs" company="RHEA System S.A.">
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

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Provides actions that are <see cref="CDP4Common.EngineeringModelData.PossibleFiniteState" /> and
    /// <see cref="CDP4Common.EngineeringModelData.ActualFiniteState" /> related
    /// </summary>
    public class StateCommand : IStateCommand
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
        /// Initialise a new <see cref="StateCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public StateCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// given actual finite state list as state dependence from selected parameters and overrides
        /// </summary>
        /// <param name="isStateDependencyToBeRemoved">the value whether the state dependency is to be removed</param>
        public void ApplyOrRemoveStateDependency(bool isStateDependencyToBeRemoved)
        {
            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("No --parameters given. Apply state dependence skipped.");
                return;
            }

            var actualFiniteStateList = this.sessionService.Iteration.ActualFiniteStateList.SingleOrDefault(x => x.ShortName == this.commandArguments.StateListName);

            if (actualFiniteStateList == null)
            {
                Console.WriteLine($"Cannot find Actual Finite State List \"{this.commandArguments.StateListName}\". Apply state dependence skipped.");
                return;
            }

            Console.WriteLine($"{(isStateDependencyToBeRemoved ? "Removing" : "Applying")} state dependency \"{this.commandArguments.StateListName}\"");

            foreach (var elementDefinition in this.sessionService.Iteration.Element.OrderBy(x => x.ShortName))
            {
                if (!this.filterService.IsFilteredIn(elementDefinition))
                {
                    continue;
                }

                // Visit all parameters in the element definitions and make the selected ones dependent on the given state if not already
                foreach (var parameter in elementDefinition.Parameter.Where(p => this.commandArguments.SelectedParameters.Contains(p.ParameterType.ShortName)).OrderBy(x => x.ParameterType.ShortName))
                {
                    //  if there is no dependency yet or another one than the one specified and the state dependency was set to be added
                    //OR
                    //  there is a state dependency set already and it is the same state as the one specified and the state dependency was set to removed
                    if ((parameter.StateDependence == null || parameter.StateDependence.Iid != actualFiniteStateList.Iid)
                        && !isStateDependencyToBeRemoved
                        || parameter.StateDependence != null && parameter.StateDependence.Iid == actualFiniteStateList.Iid
                                                             && isStateDependencyToBeRemoved)
                    {
                        var parameterClone = parameter.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone);
                        parameterClone.StateDependence = isStateDependencyToBeRemoved ? null : actualFiniteStateList;
                        transaction.CreateOrUpdate(parameterClone);
                        this.sessionService.Transactions.Add(transaction);

                        Console.WriteLine(
                            isStateDependencyToBeRemoved
                                ? $"State {this.commandArguments.StateListName} removed from Parameter {parameter.UserFriendlyShortName}, changed from {parameter.StateDependence.ShortName}"
                                : $"State {this.commandArguments.StateListName} applied to Parameter {parameter.UserFriendlyShortName}");
                    }
                    else
                    {
                        Console.WriteLine($"State {this.commandArguments.StateListName} was already {(isStateDependencyToBeRemoved ? "removed" : "applied")} to Parameter {parameter.UserFriendlyShortName}");
                    }
                }
            }
        }
    }
}
