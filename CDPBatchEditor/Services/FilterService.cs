//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="FilterService.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Linq;

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
        /// The ElementDefinition filter. The requested action is only applied to ElementDefinitions in this set.
        /// </summary>
        public HashSet<ElementDefinition> FilteredElementDefinitions { get; } = new HashSet<ElementDefinition>();

        /// <summary>
        /// The short names of the Category filter. The requested action is only applied to ElementDefinitions that are a member of
        /// these categories.
        /// </summary>
        public HashSet<string> FilteredCategoryShortNames { get; } = new HashSet<string>();

        /// <summary>
        /// The DomainOfExpertise owners filter. The requested action is only applied to ElementDefinitions owned by domains
        /// included in this set.
        /// </summary>
        public HashSet<DomainOfExpertise> IncludedOwners { get; } = new HashSet<DomainOfExpertise>();

        /// <summary>
        /// Check whether the given <see cref="ElementDefinition" /> is included in the filter.
        /// </summary>
        /// <param name="elementDefinition">
        /// The <see cref="ElementDefinition" /> to check.
        /// </param>
        /// <returns>
        /// If included returns true, otherwise false.
        /// </returns>
        public bool IsFilteredIn(ElementDefinition elementDefinition)
        {
            return this.FilteredElementDefinitions.Contains(elementDefinition) && this.IsMemberOfSelectedCategory(elementDefinition)
                                                                               && this.IncludedOwners.Contains(elementDefinition.Owner);
        }

        /// <summary>
        /// Check whether the given <see cref="ElementDefinition" /> is included in the filter. or the no element definition is
        /// specified
        /// </summary>
        /// <param name="elementDefinition">
        /// The <see cref="ElementDefinition" /> to check.
        /// </param>
        /// <returns>
        /// If included returns true, otherwise false.
        /// </returns>
        public bool IsFilteredInOrFilterIsEmpty(ElementDefinition elementDefinition)
        {
            return !this.FilteredElementDefinitions.Any() || this.IsFilteredIn(elementDefinition);
        }

        /// <summary>
        /// Check whether the given <see cref="ElementDefinition" /> is a member of the specified selected categories.
        /// </summary>
        /// <param name="elementDefinition">
        /// The <see cref="ElementDefinition" /> to check.
        /// </param>
        /// <returns>
        /// True if no categories were specified or the given Element Definition is a member, otherwise false.
        /// </returns>
        public bool IsMemberOfSelectedCategory(ElementDefinition elementDefinition)
        {
            if (!this.FilteredCategoryShortNames.Any())
            {
                return true;
            }

            var elementCategoryShortNames = elementDefinition.Category.Select(cat => cat.ShortName);

            return this.FilteredCategoryShortNames.Intersect(elementCategoryShortNames).Any();
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

            this.FilteredElementDefinitions.Clear();

            if (string.IsNullOrWhiteSpace(this.commandArguments.ElementDefinition))
            {
                this.FilteredElementDefinitions.AddRange(iteration.Element);
            }
            else
            {
                var topOfSubTreeShortName = this.commandArguments.ElementDefinition.Trim();
                var topOfSubTree = iteration.Element.FirstOrDefault(ed => ed.ShortName == topOfSubTreeShortName);

                if (topOfSubTree == null)
                {
                    Console.WriteLine($"Cannot find Element Definition with short name {topOfSubTreeShortName} for --filtered-subtree");
                }
                else
                {
                    this.CollectSubTreeElementDefinitions(topOfSubTree, this.FilteredElementDefinitions);
                }
            }

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
        /// Collect all Element Definitions contained in the subtree of a given top Element Definition.
        /// </summary>
        /// <param name="topOfSubTree">
        /// The top <see cref="ElementDefinition" /> of a subtree to be derived.
        /// </param>
        /// <param name="subTreeElementDefinitions">
        /// Set to store to the subtree Element Definitions.
        /// </param>
        private void CollectSubTreeElementDefinitions(ElementDefinition topOfSubTree, HashSet<ElementDefinition> subTreeElementDefinitions)
        {
            subTreeElementDefinitions.Add(topOfSubTree);

            foreach (var elementUsage in topOfSubTree.ContainedElement)
            {
                subTreeElementDefinitions.Add(elementUsage.ElementDefinition);

                // Recursively add the lower level subtree elements
                this.CollectSubTreeElementDefinitions(elementUsage.ElementDefinition, subTreeElementDefinitions);
            }
        }
    }
}
