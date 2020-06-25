//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ICommandArguments.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.CommandArguments.Interface
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    /// <summary>
    /// Defines an <see cref="ICommandArguments" /> that provides definitions of the command arguments
    /// </summary>
    public interface ICommandArguments
    {
        /// <summary>
        /// Gets or sets the server URI string.
        /// </summary>
        Uri ServerUri { get; }

        /// <summary>
        /// Gets or sets the short name of the engineering model to edit.
        /// <code>shortname = 'm' longName = 'model'</code>
        /// </summary>
        string EngineeringModel { get; }

        /// <summary>
        /// Gets or sets the user name
        /// <code>shortname = 'u' longName = 'user'</code>
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Gets or sets the password
        /// <code>shortname = 'p' longName = 'password'</code>
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Gets or sets the command line arguments. There are none for this tool.
        /// </summary>
        IList<string> ArgumentList { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a test run.
        /// <code>shortname = 'n' longName = 'dryrun'</code>
        /// </summary>
        bool DryRun { get; }

        /// <summary>
        /// Gets or sets the action for the batch tool to perform <see cref="CommandEnumeration" />
        /// </summary>
        CommandEnumeration Command { get; }

        /// <summary>
        /// Gets or sets a list of short names of selected parameters.
        /// </summary>
        IReadOnlyList<string> SelectedParameters { get; }

        /// <summary>
        /// Gets or sets a list of short names of categories to be used as a filter.
        /// </summary>
        IList<string> FilteredCategories { get; }

        /// <summary>
        /// Gets or sets the short name of an element definition to be used as the top node of sub-tree filter.
        /// </summary>
        string ElementDefinition { get; }

        /// <summary>
        /// Gets or sets a list of short names of domains of expertise to be included as a filter.
        /// </summary>
        IList<string> IncludedOwners { get; }

        /// <summary>
        /// Gets or sets a list of short names of domains of expertise to be included as a filter.
        /// </summary>
        IList<string> ExcludedOwners { get; }

        /// <summary>
        /// Gets or sets the short name of a selected domain of expertise.
        /// </summary>
        string DomainOfExpertise { get; }

        /// <summary>
        /// Gets or sets the short name of a selected domain of expertise to change to.
        /// </summary>
        string ToDomainOfExpertise { get; }

        /// <summary>
        /// Gets or sets the short name of the actual finite state list
        /// </summary>
        string StateListName { get; }

        /// <inheritdoc cref="CDP4Common.EngineeringModelData.ParameterSwitchKind" />
        /// <summary>
        /// Gets or sets the switch on all subscriptions on parameters or parameter overrides owned by --subscriber.
        /// </summary>
        ParameterSwitchKind? ParameterSwitchKind { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to report the element definitions, parameters and parameter subscriptions in a
        /// CSV file.
        /// </summary>
        bool Report { get; }

        /// <summary>
        /// Gets or sets the name of the parameter group into which new parameters will be added.
        /// </summary>
        string ParameterGroup { get; }

        /// <summary>
        /// Gets or sets the short name of the scale to be assigned to the selected parameters.
        /// </summary>
        string Scale { get; }

        /// <summary>
        /// Gets or sets the the iteration short name to work on
        /// </summary>
        string Iteration { get; }
    }
}
