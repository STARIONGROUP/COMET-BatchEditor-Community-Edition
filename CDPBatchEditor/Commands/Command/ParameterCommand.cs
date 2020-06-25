//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ParameterCommand.cs" company="RHEA System S.A.">
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

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Provides actions that apply to <see cref="Parameter" />
    /// </summary>
    public class ParameterCommand : IParameterCommand
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
        /// Initialise a new <see cref="ParameterCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public ParameterCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Adds <see cref="Parameter" />s to all <see cref="ElementDefinition" />s of the given
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

                foreach (var elementDefinition in this.sessionService.Iteration.Element
                    .Where(x => this.filterService.IsFilteredIn(x))
                    .OrderBy(x => x.ShortName))
                {
                    var elementDefinitionClone = elementDefinition.Clone(true);

                    this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(elementDefinitionClone), elementDefinitionClone));

                    foreach (var parameterType in selectedParameterTypes)
                    {
                        // Add parameter if it does not exist
                        var parameter = elementDefinition.Parameter.FirstOrDefault(p => p.ParameterType == parameterType);

                        if (parameter == null)
                        {
                            var parameterOwner = owner ?? elementDefinition.Owner;
                            var defaultScale = !(parameterType is QuantityKind quantityKind) ? null : quantityKind.DefaultScale;

                            parameter = new Parameter(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                                { Owner = parameterOwner, ParameterType = parameterType, Scale = defaultScale };

                            this.sessionService.Transactions.Last().Create(parameter, elementDefinitionClone);
                            Console.WriteLine($"In {elementDefinition.ShortName} added Parameter {parameterType.ShortName}");
                        }
                        else
                        {
                            parameter = parameter.Clone(true);
                        }

                        // If ParameterGroup specified then move Parameter to that group
                        if (!string.IsNullOrEmpty(this.commandArguments.ParameterGroup))
                        {
                            var parameterGroup = this.GetOrCreateParameterGroup(elementDefinitionClone, this.commandArguments.ParameterGroup, this.sessionService.Transactions.Last());
                            this.MoveParameterToGroup(parameter, parameterGroup, this.sessionService.Transactions.Last());
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

            foreach (var elementDefinition in this.sessionService.Iteration.Element.OrderBy(x => x.ShortName))
            {
                if (!this.filterService.IsFilteredIn(elementDefinition))
                {
                    continue;
                }

                var elementDefinitionClone = elementDefinition.Clone(true);

                this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(elementDefinitionClone), elementDefinitionClone));

                foreach (var parameterType in selectedParameterTypes)
                {
                    // Remove parameter if it exists and if owner is given, owned by given domain
                    var parameter = elementDefinition.Parameter.FirstOrDefault(p => p.ParameterType == parameterType);

                    if (parameter != null && (owner == null || parameter.Owner == owner))
                    {
                        var parameterClone = parameter.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone);
                        transaction.Delete(parameter, elementDefinitionClone);
                        this.sessionService.Transactions.Add(transaction);
                        Console.WriteLine($"In {elementDefinition.ShortName} removed Parameter {parameterType.ShortName}");
                    }
                }
            }
        }

        /// <summary>
        /// Move the parameter to the given parameter group.
        /// </summary>
        /// <param name="parameter">The <see cref="Parameter" /> whose group will change</param>
        /// <param name="group">The <see cref="ParameterGroup" /></param>
        /// <param name="transaction">the <see cref="ThingTransaction" /> holding the changes to persist</param>
        private void MoveParameterToGroup(ParameterBase parameter, ParameterGroup group, ThingTransaction transaction)
        {
            if (parameter != null && group != null && parameter.Group != group)
            {
                parameter.Group = group;
                transaction.CreateOrUpdate(parameter);
                Console.WriteLine($"Moved parameter {parameter.ParameterType.ShortName} to group {group.Name}");
            }
        }

        /// <summary>
        /// Get parameter group with given name. If it does not exist create it.
        /// </summary>
        /// <param name="elementDefinition">he element definition.</param>
        /// <param name="parameterGroupName">The parameter group name.</param>
        /// <param name="transaction">the <see cref="ThingTransaction" /> holding the changes to persist</param>
        /// <returns>The <see cref="ParameterGroup" /></returns>
        private ParameterGroup GetOrCreateParameterGroup(ElementDefinition elementDefinition, string parameterGroupName, ThingTransaction transaction)
        {
            var parameterGroup = elementDefinition.ParameterGroup.SingleOrDefault(pg => pg.Name == parameterGroupName);

            if (parameterGroup == null)
            {
                parameterGroup = new ParameterGroup(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri) { Name = parameterGroupName };
                transaction.Create(parameterGroup, elementDefinition);
            }

            return parameterGroup.Iid != Guid.Empty ? parameterGroup.Clone(true) : parameterGroup;
        }
    }
}
