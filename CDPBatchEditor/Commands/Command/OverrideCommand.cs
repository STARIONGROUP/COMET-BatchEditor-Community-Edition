//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="OverrideCommand.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2024 Starion Group S.A.
// 
//     Author: Nathanael Smiechowski, Alex Vorobiev, Alexander van Delft, Sam Gerené
// 
//     This file is part of CDP4-COMET Batch Editor.
//     The CDP4-COMET Batch Editor is a commandline application to perform batch operations on a
//     ECSS-E-TM-10-25 Annex A and Annex C data source
// 
//     The CDP4-COMET Batch Editor is free software; you can redistribute it and/or
//     modify it under the terms of the GNU Lesser General Public
//     License as published by the Free Software Foundation; either
//     version 3 of the License, or any later version.
// 
//     The CDP4-COMET Batch Editor is distributed in the hope that it will be useful,
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
    /// Defines an <see cref="OverrideCommand" /> that provides actions that are
    /// <see cref="CDP4Common.EngineeringModelData.ParameterOverride" /> related
    /// </summary>
    public class OverrideCommand : IOverrideCommand
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
        /// Initialise a new <see cref="OverrideCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public OverrideCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Override parameters with given short names, possibly filtered on Ownership (Included Owner) or Category.
        /// </summary>
        public void Override()
        {
            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("No --parameters given. Override parameters skipped.");
                return;
            }

            var allElementUsages = this.sessionService.Iteration.Element.SelectMany(x => x.ContainedElement).ToArray();

            foreach (var elementDefinition in this.sessionService.Iteration.Element
                .Where(e => this.filterService.IsFilteredIn(e))
                .OrderBy(x => x.ShortName).ToArray())
            {
                // From 10-25 docs:
                // Note 2: The owner DomainOfExpertise of this ParameterOverride is the same as the owner of the elementDefinition. 
                var overrider = elementDefinition.Owner;

                foreach (var elementDefinitionParameter in elementDefinition.Parameter)
                {
                    if (this.commandArguments.SelectedParameters.Contains(elementDefinitionParameter.ParameterType.ShortName))
                    {
                        var specificElementUsages = allElementUsages.Where(x => x.ElementDefinition == elementDefinition).ToArray();

                        foreach (var elementUsage in specificElementUsages.OrderBy(x => x.ShortName))
                        {
                            if (elementUsage.ParameterOverride.All(x => x.Parameter != elementDefinitionParameter))
                            {
                                var elementUsageClone = elementUsage.Clone(true);

                                this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(elementUsageClone), elementUsageClone));

                                var parameterOverride = new ParameterOverride(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                                {
                                    Owner = overrider, 
                                    Parameter = elementDefinitionParameter
                                };

                                this.sessionService.Transactions.Last().Create(parameterOverride, elementUsageClone);

                                Console.WriteLine($"Parameter Override created on {parameterOverride.UserFriendlyShortName}");
                            }
                        }
                    }
                }
            }
        }
    }
}
