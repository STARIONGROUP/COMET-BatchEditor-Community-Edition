//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="RequirementSimpleParameterValueCommand.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2025 Starion Group S.A.
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
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.Helpers;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Provides actions that apply a <see cref="SimpleParameterValue" /> to a <see cref="Requirement"/>
    /// </summary>
    public class RequirementSimpleParameterValueCommand : IRequirementSimpleParameterValueCommand
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
        /// Initialise a new <see cref="RequirementSimpleParameterValueCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public RequirementSimpleParameterValueCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Adds <see cref="SimpleParameterValue" />s to all <see cref="SimpleParameterizableThing" />s of the given
        /// <see cref="CDP4Common.EngineeringModelData.Iteration" />.
        /// </summary>
        public void Add()
        {
            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("Command add-parameters: No --parameters given.");
                return;
            }

            var selectedParameterTypes = new List<ParameterType>();

            foreach (var selectedParameter in this.commandArguments.SelectedParameters)
            {
                var parameterType = this.sessionService.Iteration.RequiredRdls.Select(pt => pt.ParameterType.FirstOrDefault(p => p.ShortName == selectedParameter)).SingleOrDefault(p => p != null && p.ShortName == selectedParameter);

                if (parameterType == null)
                {
                    Console.WriteLine($"Command add-parameters: parameter type with short name \"{selectedParameter}\" not found.");
                }
                else
                {
                    selectedParameterTypes.Add(parameterType);
                }
            }

            if (selectedParameterTypes.Count != 0)
            {
                DomainOfExpertise owner = null;

                if (!string.IsNullOrEmpty(this.commandArguments.DomainOfExpertise))
                {
                    owner = this.sessionService.SiteDirectory.Domain.SingleOrDefault(d => d.ShortName == this.commandArguments.DomainOfExpertise);

                    if (owner == null)
                    {
                        Console.WriteLine("Command add-parameters: domain-of-expertise with short name \"{0}\" not found.", this.commandArguments.DomainOfExpertise);
                        return;
                    }
                }

                foreach (var requirement in this.sessionService.Iteration.RequirementsSpecification.SelectMany(x => x.Requirement)
                    .Where(x => this.filterService.IsFilteredIn(x))
                    .OrderBy(x => x.ShortName))
                {
                    var requirementClone = requirement.Clone(true);

                    this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(requirementClone), requirementClone));

                    foreach (var parameterType in selectedParameterTypes)
                    {
                        // Add parameter if it does not exist
                        var parameter = requirement.ParameterValue.FirstOrDefault(p => p.ParameterType == parameterType);

                        if (parameter == null)
                        {
                            var defaultScale = !(parameterType is QuantityKind quantityKind) ? null : quantityKind.DefaultScale;

                            var defaultValue = ValueArrayUtils.CreateDefaultValueArray(parameterType.NumberOfValues);

                            parameter = new SimpleParameterValue(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                                { ParameterType = parameterType, Scale = defaultScale, Value = defaultValue };

                            this.sessionService.Transactions.Last().Create(parameter, requirementClone);
                            Console.WriteLine($"In {requirement.ShortName} added SimpleParameterValue {parameterType.ShortName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove one or more parameter from the specified <see cref="ElementDefinition" />
        /// </summary>
        public void Remove()
        {
            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("Action remove-parameters: No --parameters given.");
                return;
            }

            var aggregateRdl = this.sessionService.Iteration.RequiredRdls.ToList();
            var selectedParameterTypes = new List<ParameterType>();

            foreach (var selectedParameter in this.commandArguments.SelectedParameters)
            {
                var parameterType = aggregateRdl.SelectMany(pt => pt.ParameterType).SingleOrDefault(pt => pt.ShortName == selectedParameter);

                if (parameterType == null)
                {
                    Console.WriteLine($"Action remove-parameters: parameter type with short name \"{selectedParameter}\" not found.");
                }
                else
                {
                    selectedParameterTypes.Add(parameterType);
                }
            }

            if (!selectedParameterTypes.Any())
            {
                return;
            }

            DomainOfExpertise owner = null;

            if (!string.IsNullOrEmpty(this.commandArguments.DomainOfExpertise))
            {
                owner = this.sessionService.SiteDirectory.Domain.FirstOrDefault(domainOfExpertise => domainOfExpertise.ShortName == this.commandArguments.DomainOfExpertise);

                if (owner == null)
                {
                    Console.WriteLine($"Action remove-parameters: domain-of-expertise with short name \"{this.commandArguments.DomainOfExpertise}\" not found.");
                    return;
                }
            }

            foreach (var requirement in this.sessionService.Iteration.RequirementsSpecification.SelectMany(x => x.Requirement).OrderBy(x => x.ShortName))
            {
                if (!this.filterService.IsFilteredIn(requirement))
                {
                    continue;
                }

                var requirementClone = requirement.Clone(true);

                this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(requirementClone), requirementClone));

                foreach (var parameterType in selectedParameterTypes)
                {
                    // Remove parameter if it exists and if owner is given, owned by given domain
                    var parameter = requirement.ParameterValue.FirstOrDefault(p => p.ParameterType == parameterType);

                    if (parameter != null && (owner == null || parameter.Owner == owner))
                    {
                        var parameterClone = parameter.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone);
                        transaction.Delete(parameter, requirementClone);
                        this.sessionService.Transactions.Add(transaction);
                        Console.WriteLine($"In {requirement.ShortName} removed Parameter {parameterType.ShortName}");
                    }
                }
            }
        }
    }
}
