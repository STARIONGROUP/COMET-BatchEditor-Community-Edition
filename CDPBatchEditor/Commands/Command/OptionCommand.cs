//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="OptionCommand.cs" company="RHEA System S.A.">
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
    /// Provides actions that are <see cref="Option" /> related
    /// </summary>
    public class OptionCommand : IOptionCommand
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
        /// Initialise a new <see cref="OptionCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public OptionCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Apply option dependence or remove it to/from selected parameters and overrides for element definitions and usages of
        /// selected categories.
        /// </summary>
        /// <param name="isOptionDependencyToBeRemoved">the value whether the option dependency is to be removed</param>
        public void ApplyOrRemoveOptionDependency(bool isOptionDependencyToBeRemoved)
        {
            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("No --parameters given. Apply option dependence skipped.");
                return;
            }

            foreach (var elementDefinition in this.sessionService.Iteration.Element.OrderBy(x => x.ShortName))
            {
                if (!this.filterService.IsFilteredIn(elementDefinition))
                {
                    continue;
                }

                // Apply option dependence to the selected parameters
                foreach (var parameter in elementDefinition.Parameter.OrderBy(x => x.ParameterType.ShortName))
                {
                    if (this.commandArguments.SelectedParameters.Contains(parameter.ParameterType.ShortName))
                    {
                        if (!parameter.IsOptionDependent && !isOptionDependencyToBeRemoved
                            || parameter.IsOptionDependent && isOptionDependencyToBeRemoved)
                        {
                            var parameterClone = parameter.Clone(true);
                            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone);
                            parameterClone.IsOptionDependent = !parameter.IsOptionDependent;
                            transaction.CreateOrUpdate(parameterClone);
                            this.sessionService.Transactions.Add(transaction);

                            Console.WriteLine($"Parameter {parameter.UserFriendlyShortName} made {(!parameter.IsOptionDependent ? "" : "not")} option dependent");
                        }
                        else
                        {
                            Console.WriteLine($"Parameter {parameter.UserFriendlyShortName} was already {(isOptionDependencyToBeRemoved ? "not" : "")} option dependent");
                        }
                    }
                }
            }
        }
    }
}
