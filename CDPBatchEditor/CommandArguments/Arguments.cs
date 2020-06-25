//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Arguments.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.CommandArguments
{
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments.Interface;

    using CommandLine;

    /// <summary>
    /// Command line options, <see href="https://github.com/gsscoder/commandline/wiki/Quickstart" /> for documentation.
    /// </summary>
    public class Arguments : ConnectionArguments, ICommandArguments
    {
        /// <summary>
        /// Gets or sets the command line arguments. There are none for this tool.
        /// </summary>
        [Value(0)]
        public IList<string> ArgumentList { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a dry run.
        /// <code>longName = 'dry'</code>
        /// </summary>
        [Option(
            "dry",
            Required = false,
            Default = false,
            HelpText = "Perform a dry run, i.e. do not commit any changes to persistent data store.")]
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets the action for the batch tool to perform <see cref="CommandEnumeration" />
        /// <code>longName = 'action'</code>
        /// </summary>
        [Option("action", Required = false,
            HelpText = "Batch action to perform on the engineering model(s). Possible value is one of:"
                       + "AddParameters,"
                       + "RemoveParameters,"
                       + "MoveReferenceValuesToManualValues,"
                       + "ApplyOptionDependence,"
                       + "ApplyStateDependence,"
                       + "ChangeParameterOwnership,"
                       + "ChangeDomain,"
                       + "RemoveOptionDependence,"
                       + "RemoveStateDependence,"
                       + "SetGenericOwners,"
                       + "SetScale,"
                       + "SetShapeScaleMm,"
                       + "SetSubscriptionSwitch,"
                       + "Subscribe")]
        public CommandEnumeration Command { get; set; }

        /// <summary>
        /// Gets or sets a list of short names of selected parameters.
        /// </summary>
        [Option(
            "parameters",
            Separator = ',',
            Required = false,
            HelpText = "Comma-separated list of short names of parameters. "
                       + "Use in conjunction with --action=add-parameters | remove-parameters | change-parameter-ownership | subscribe "
                       + "| apply-state-dependence | remove-state-dependence | apply-option-dependence | remove-option-dependence.")]
        public IReadOnlyList<string> SelectedParameters { get; set; }

        /// <summary>
        /// Gets or sets a list of short names of categories to be used as a filter.
        /// </summary>
        [Option(
            "categories",
            Separator = ',',
            Required = false,
            HelpText = "Comma-separated list of short names of categories. Use in conjunction with --action. "
                       + "The specified action will only be applied to Element Definitions that are a member of at least one of the given categories.")]
        public IList<string> FilteredCategories { get; set; }

        /// <summary>
        /// Gets or sets the short name of an element definition to be used as the top node of sub-tree filter.
        /// </summary>
        [Option(
            "element-definition",
            Required = false,
            HelpText = "Short name of an Element Definition that sets the top node of a decomposition subtree. Use in conjunction with --action. "
                       + "The specified action will only be applied to the given Element Definition and its subtree of contained Element Definitions.")]
        public string ElementDefinition { get; set; }

        /// <summary>
        /// Gets or sets a list of short names of domains of expertise to be included as a filter.
        /// </summary>
        [Option(
            "included-owners",
            Separator = ',',
            Required = false,
            HelpText = "Comma-separated list of short names of domains of expertise. Use in conjunction with --action. "
                       + "The specified action will only be applied to Element Definitions owned by one of the given domains. "
                       + "If not specified all domains will be included.")]
        public IList<string> IncludedOwners { get; set; }

        /// <summary>
        /// Gets or sets a list of short names of domains of expertise to be included as a filter.
        /// </summary>
        [Option(
            "excluded-owners",
            Separator = ',',
            Required = false,
            HelpText = "Comma-separated list of short names of domains of expertise. Use in conjunction with --action. "
                       + "The specified action will only be applied to Element Definitions not owned by one of the given domains. "
                       + "The --included-owners will be processed before the --excluded-owners.")]
        public IList<string> ExcludedOwners { get; set; }

        /// <summary>
        /// Gets or sets the short name of a selected domain of expertise.
        /// </summary>
        [Option(
            "domain",
            Required = false,
            HelpText = "Short name of a domain-of-expertise owner or subscriber. "
                       + "Use in conjunction with --action=add-parameters | change-parameter-ownership | set-subscription-switch | subscribe.")]
        public string DomainOfExpertise { get; set; }

        /// <summary>
        /// Gets or sets the short name of a selected domain of expertise to change to.
        /// </summary>
        [Option(
            "to-domain",
            Required = false,
            HelpText = "Short name of a domain-of-expertise to change to. " + "Use in conjunction with --change-domain.")]
        public string ToDomainOfExpertise { get; set; }

        /// <summary>
        /// Gets or sets the short name of the actual finite state list
        /// </summary>
        [Option(
            "state",
            Required = false,
            HelpText = "Short name of an actual finite state list on which to make parameters state dependent. "
                       + "Use in conjunction with --action=apply-state-dependence and --parameters.")]
        public string StateListName { get; set; }

        /// <summary>
        /// Gets or sets the switch on all subscriptions on parameters or parameter overrides owned by --subscriber.
        /// </summary>
        [Option(
            "parameter-switch",
            Required = false,
            HelpText = "Set switch on all subscriptions on parameters or parameter overrides owned by --domain. "
                       + "Possible value is: COMPUTED | MANUAL | REFERENCE.")]
        public ParameterSwitchKind? ParameterSwitchKind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to report the element definitions, parameters and parameter subscriptions in a
        /// CSV file.
        /// </summary>
        [Option(
            "report",
            Required = false,
            Default = false,
            HelpText = "Report the element definitions, parameters and parameter subscriptions in a CSV file.")]
        public bool Report { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter group into which new parameters will be added.
        /// </summary>
        [Option(
            "parameter-group",
            Required = false,
            HelpText = "Name of a parameter group into which new parameters will be added. "
                       + "If it does not exist, the parameter group will be created. " + "Use in conjunction with --action=add-parameters.")]
        public string ParameterGroup { get; set; }

        /// <summary>
        /// Gets or sets the short name of the scale to be assigned to the selected parameters.
        /// </summary>
        [Option(
            "scale",
            Required = false,
            HelpText = "Short name of a measurement scale to be assigned to selected parameters. "
                       + "The assignment will only be performed if the scale is one of the possible scales of the parameter. "
                       + "Use in conjunction with --action=set-scale and --parameters.")]
        public string Scale { get; set; }

        /// <summary>
        /// Gets or sets the the iteration short name to work on
        /// </summary>
        [Option(
            'i',
            "iteration",
            Required = false,
            HelpText = "The iteration short name to work on")]
        public string Iteration { get; set; }
    }
}
