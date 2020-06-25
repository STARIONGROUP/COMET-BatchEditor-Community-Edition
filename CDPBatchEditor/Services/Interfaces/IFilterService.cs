//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IFilterService.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Services.Interfaces
{
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    /// <summary>
    /// Defines an <see cref="IFilterService" /> to provide filters accomodations
    /// </summary>
    public interface IFilterService
    {
        /// <summary>
        /// The ElementDefinition filter. The requested action is only applied to ElementDefinitions in this set.
        /// </summary>
        HashSet<ElementDefinition> FilteredElementDefinitions { get; }

        /// <summary>
        /// The short names of the Category filter. The requested action is only applied to ElementDefinitions that are a member of
        /// these categories.
        /// </summary>
        HashSet<string> FilteredCategoryShortNames { get; }

        /// <summary>
        /// The DomainOfExpertise owners filter. The requested action is only applied to ElementDefinitions owned by domains
        /// included in this set.
        /// </summary>
        HashSet<DomainOfExpertise> IncludedOwners { get; }

        /// <summary>
        /// Check whether the given <see cref="ElementDefinition" /> is included in the filter.
        /// </summary>
        /// <param name="elementDefinition">
        /// The <see cref="ElementDefinition" /> to check.
        /// </param>
        /// <returns>
        /// If included returns true, otherwise false.
        /// </returns>
        bool IsFilteredIn(ElementDefinition elementDefinition);

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
        bool IsFilteredInOrFilterIsEmpty(ElementDefinition elementDefinition);

        /// <summary>
        /// Check whether the given <see cref="ElementDefinition" /> is a member of the specified selected categories.
        /// </summary>
        /// <param name="elementDefinition">
        /// The <see cref="ElementDefinition" /> to check.
        /// </param>
        /// <returns>
        /// True if no categories were specified or the given Element Definition is a member, otherwise false.
        /// </returns>
        bool IsMemberOfSelectedCategory(ElementDefinition elementDefinition);

        /// <summary>
        /// Process provided filtered Category, Domain of expertise and element definitions
        /// </summary>
        /// <param name="iteration">The Selected <see cref="Iteration" /></param>
        /// <param name="allSiteDirectoryDomain">The list of domain existing in the site directory</param>
        void ProcessFilters(Iteration iteration, IList<DomainOfExpertise> allSiteDirectoryDomain);

        /// <summary>
        /// Verify if the current parameter is specified in the command line arguments or none was specified
        /// </summary>
        /// <param name="parameter">The parameter to check against</param>
        /// <returns>Assert whether the current parameter is specified in the command line arguments or none was specified</returns>
        bool IsParameterSpecifiedOrAny(Parameter parameter);
    }
}
