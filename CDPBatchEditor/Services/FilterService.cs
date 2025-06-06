//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="FilterService.cs" company="Starion Group S.A.">
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

namespace CDPBatchEditor.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Extensions;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Represent a service that provides filters based on specified arguments provided
    /// </summary>
    public class FilterService : IFilterService
    {
        /// <summary>
        /// Command arguments
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// Initialises a new instance of the <see cref="FilterService" /> class.
        /// </summary>
        /// <param name="commandArguments">The command arguments</param>
        public FilterService(ICommandArguments commandArguments)
        {
            this.commandArguments = commandArguments;
        }

        /// <summary>
        /// The ElementDefinition filter. The requested action is only applied to ElementDefinition in this set.
        /// </summary>
        public HashSet<DefinedThing> FilteredDefinedThings { get; } = new();

        /// <summary>
        /// The short names of the Category filter. The requested action is only applied to Things that are a member of
        /// these categories.
        /// </summary>
        public HashSet<string> FilteredCategoryShortNames { get; } = new();

        /// <summary>
        /// The DomainOfExpertise owners filter. The requested action is only applied to Things owned by domains
        /// included in this set.
        /// </summary>
        public HashSet<DomainOfExpertise> IncludedOwners { get; } = new();

        /// <summary>
        /// Check whether the given <see cref="DefinedThing" /> is included in the filter.
        /// </summary>
        /// <param name="definedThing">
        /// The <see cref="DefinedThing" /> to check.
        /// </param>
        /// <returns>
        /// If included returns true, otherwise false.
        /// </returns>
        public bool IsFilteredIn<T>(T definedThing) where T : DefinedThing, ICategorizableThing, IOwnedThing
        {
            return this.FilteredDefinedThings.Contains(definedThing) && this.IsMemberOfSelectedCategory(definedThing)
                                                                     && this.IncludedOwners.Contains(definedThing.Owner);
        }

        /// <summary>
        /// Check whether the given <see cref="DefinedThing" /> is included in the filter. or the no <see cref="DefinedThing" /> is
        /// specified
        /// </summary>
        /// <param name="definedThing">
        /// The <see cref="DefinedThing" /> to check.
        /// </param>
        /// <returns>
        /// If included returns true, otherwise false.
        /// </returns>
        public bool IsFilteredInOrFilterIsEmpty<T>(T definedThing) where T : DefinedThing, ICategorizableThing, IOwnedThing
        {
            return !this.FilteredDefinedThings.Any() || this.IsFilteredIn(definedThing);
        }

        /// <summary>
        /// Check whether the given <see cref="ICategorizableThing" /> is a member of the specified selected categories.
        /// </summary>
        /// <param name="categorizableThing">
        /// The <see cref="ICategorizableThing" /> to check.
        /// </param>
        /// <returns>
        /// True if no categories were specified or the given <see cref="ICategorizableThing"/>> is a member, otherwise false.
        /// </returns>
        public bool IsMemberOfSelectedCategory<T>(T categorizableThing) where T : ICategorizableThing
        {
            if (!this.FilteredCategoryShortNames.Any())
            {
                return true;
            }

            var categorizableThingCategoryShortNames = categorizableThing.Category.Select(cat => cat.ShortName);

            return this.FilteredCategoryShortNames.Intersect(categorizableThingCategoryShortNames).Any();
        }

        /// <summary>
        /// Process provided filtered Category, Domain of expertise and element definitions
        /// </summary>
        /// <param name="iteration">The Selected <see cref="Iteration" /></param>
        /// <param name="allSiteDirectoryDomain">The list of domain existing in the site directory</param>
        public void ProcessFilters(Iteration iteration, IList<DomainOfExpertise> allSiteDirectoryDomain)
        {
            this.FilteredCategoryShortNames.Clear();

            this.FilteredCategoryShortNames.AddRange(this.commandArguments.FilteredCategories?.Select(n => n.Trim()));

            this.FilteredDefinedThings.Clear();

            this.ProcessElementDefinitionFilters(iteration);
            this.ProcessRequirementFilters(iteration);

            this.IncludedOwners.Clear();

            this.IncludedOwners.AddRange(
                !this.commandArguments.IncludedOwners.Any()
                    ? allSiteDirectoryDomain
                    : allSiteDirectoryDomain.Where(d => this.commandArguments.IncludedOwners.Contains(d.ShortName)));

            if (this.commandArguments.ExcludedOwners.Any())
            {
                this.IncludedOwners.RemoveWhere(d => this.commandArguments.ExcludedOwners.Contains(d.ShortName));
            }
        }

        /// <summary>
        /// Process provided filtered Category, Domain of expertise and element definitions
        /// </summary>
        /// <param name="iteration">The Selected <see cref="Iteration" /></param>
        private void ProcessElementDefinitionFilters(Iteration iteration)
        {
            if (string.IsNullOrWhiteSpace(this.commandArguments.ElementDefinition))
            {
                this.FilteredDefinedThings.AddRange(iteration.Element);
            }
            else
            {
                var topOfSubTreeShortName = this.commandArguments.ElementDefinition.Trim();
                var topOfSubTree = iteration.Element.FirstOrDefault(ed => ed.ShortName == topOfSubTreeShortName);

                if (topOfSubTree == null)
                {
                    Console.WriteLine($"Cannot find Element Definition with short name {topOfSubTreeShortName} for --element-definition");
                }
                else
                {
                       this.CollectSubTreeElementDefinitions(topOfSubTree);
                }
            }
        }

        /// <summary>
        /// Process provided filtered Category, Domain of expertise and requirements
        /// </summary>
        /// <param name="iteration">The Selected <see cref="Iteration" /></param>
        private void ProcessRequirementFilters(Iteration iteration)
        {
            if (string.IsNullOrWhiteSpace(this.commandArguments.RequirementsSpecification))
            {
                this.FilteredDefinedThings.AddRange(iteration.RequirementsSpecification.SelectMany(x => x.Requirement));
            }
            else
            {
                var topOfSubTreeShortName = this.commandArguments.RequirementsSpecification.Trim();
                var topOfSubTree = iteration.RequirementsSpecification.FirstOrDefault(ed => ed.ShortName == topOfSubTreeShortName);

                if (topOfSubTree == null)
                {
                    Console.WriteLine($"Cannot find Requirements Specification with short name {topOfSubTreeShortName} for --requirements-specification");
                }
                else
                {
                    this.FilteredDefinedThings.AddRange(topOfSubTree.Requirement);
                }
            }
        }

        /// <summary>
        /// Verify if the current parameter is specified in the command line arguments or none was specified
        /// </summary>
        /// <param name="parameter">The parameter to check against</param>
        /// <returns>Assert whether the current parameter is specified in the command line arguments or none was specified</returns>
        public bool IsParameterSpecifiedOrAny(Parameter parameter)
        {
            var isNotEmpty = this.commandArguments.SelectedParameters.Any();
            return !isNotEmpty || this.commandArguments.SelectedParameters.Contains(parameter.ParameterType.ShortName);
        }

        /// <summary>
        /// Verify if the current <see cref="SimpleParameterValue"/> is specified in the command line arguments or none was specified
        /// </summary>
        /// <param name="simpleParameterValue">The <see cref="SimpleParameterValue"/> to check against</param>
        /// <returns>Assert whether the current parameter is specified in the command line arguments or none was specified</returns>
        public bool IsParameterSpecifiedOrAny(SimpleParameterValue simpleParameterValue)
        {
            var isNotEmpty = this.commandArguments.SelectedParameters.Any();
            return !isNotEmpty || this.commandArguments.SelectedParameters.Contains(simpleParameterValue.ParameterType.ShortName);
        }

        /// <summary>
        /// Collect all Element Definitions contained in the subtree of a given top Element Definition.
        /// </summary>
        /// <param name="topOfSubTree">
        /// The top <see cref="ElementDefinition" /> of a subtree to be derived.
        /// </param>
        private void CollectSubTreeElementDefinitions(ElementDefinition topOfSubTree)
        {
            this.FilteredDefinedThings.Add(topOfSubTree);

            foreach (var elementUsage in topOfSubTree.ContainedElement)
            {
                this.FilteredDefinedThings.Add(elementUsage.ElementDefinition);

                // Recursively add the lower level subtree elements
                this.CollectSubTreeElementDefinitions(elementUsage.ElementDefinition);
            }
        }
    }
}
