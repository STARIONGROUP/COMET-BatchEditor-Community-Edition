//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ElementUsageCommand.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2025 Starion Group S.A.
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
    using System.IO;
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Defines an <see cref="ElementUsageCommand" /> that provides actions that are <see cref="ElementUsage" /> related.
    /// </summary>
    public class ElementUsageCommand : IElementUsageCommand
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
        /// Per <see cref="ElementUsage" /> lines describing what was synchronised, written to the sync report.
        /// </summary>
        private readonly List<string> reportEntries = new();

        /// <summary>
        /// Initialise a new <see cref="ElementUsageCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public ElementUsageCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Synchronises (overwrites) the <see cref="ElementUsage.ShortName" /> and <see cref="ElementUsage.Name" /> of
        /// every <see cref="ElementUsage" /> with the <see cref="ElementDefinition.ShortName" /> and
        /// <see cref="ElementDefinition.Name" /> of the <see cref="ElementDefinition" /> it references. Only usages whose
        /// referenced element definition passes the active filters (subtree, category and owner) are considered, and a
        /// usage is only rewritten when its short name or name actually differs from the referenced element definition.
        /// </summary>
        public void SyncNames()
        {
            var iteration = this.sessionService.Iteration;

            if (iteration == null)
            {
                Console.WriteLine("SyncElementUsageNames: no iteration is available. Aborting.");
                return;
            }

            var allElementUsages = iteration.Element.SelectMany(elementDefinition => elementDefinition.ContainedElement).ToArray();

            // The referenced element definition provides the names, so scope on it (mirroring the OverrideCommand): for
            // every element definition in scope, all usages that point to it are synchronised.
            foreach (var elementDefinition in iteration.Element
                .Where(elementDefinition => this.filterService.IsFilteredIn(elementDefinition))
                .OrderBy(elementDefinition => elementDefinition.ShortName))
            {
                foreach (var elementUsage in allElementUsages
                    .Where(usage => usage.ElementDefinition == elementDefinition)
                    .OrderBy(usage => usage.ShortName))
                {
                    this.SyncElementUsage(elementUsage, elementDefinition);
                }
            }

            this.WriteReport();
        }

        /// <summary>
        /// Synchronises a single <see cref="ElementUsage" /> with its referenced <see cref="ElementDefinition" />. Nothing
        /// is written (and nothing is reported) when the usage already matches the element definition.
        /// </summary>
        /// <param name="elementUsage">The <see cref="ElementUsage" /> to synchronise.</param>
        /// <param name="elementDefinition">The referenced <see cref="ElementDefinition" /> that provides the names.</param>
        private void SyncElementUsage(ElementUsage elementUsage, ElementDefinition elementDefinition)
        {
            var shortNameChanged = elementUsage.ShortName != elementDefinition.ShortName;
            var nameChanged = elementUsage.Name != elementDefinition.Name;

            if (!shortNameChanged && !nameChanged)
            {
                return;
            }

            var changes = new List<string>();

            if (shortNameChanged)
            {
                changes.Add($"shortName '{elementUsage.ShortName}' -> '{elementDefinition.ShortName}'");
            }

            if (nameChanged)
            {
                changes.Add($"name '{elementUsage.Name}' -> '{elementDefinition.Name}'");
            }

            var elementUsageClone = elementUsage.Clone(false);
            elementUsageClone.ShortName = elementDefinition.ShortName;
            elementUsageClone.Name = elementDefinition.Name;

            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(elementUsageClone), elementUsageClone);
            transaction.CreateOrUpdate(elementUsageClone);
            this.sessionService.Transactions.Add(transaction);

            var containerShortName = (elementUsage.Container as ElementDefinition)?.ShortName;
            this.reportEntries.Add($"[USAGE UPDATED] {containerShortName}.{elementUsage.ShortName}: {string.Join(", ", changes)}");
            Console.WriteLine($"Synchronised Element Usage {containerShortName}.{elementUsage.ShortName}: {string.Join(", ", changes)}");
        }

        /// <summary>
        /// Writes the sync report to a LastElementUsageNameSyncReport.txt file in the current working directory,
        /// overwriting any existing file.
        /// </summary>
        private void WriteReport()
        {
            const string fileName = "LastElementUsageNameSyncReport.txt";

            var lines = new List<string>
            {
                $"Last Element Usage Name Sync Report - generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Engineering model : {this.commandArguments.EngineeringModel}",
                $"Element definition filter : {(string.IsNullOrWhiteSpace(this.commandArguments.ElementDefinition) ? "none" : this.commandArguments.ElementDefinition)}",
                $"Categories filter : {(this.commandArguments.FilteredCategories.Any() ? string.Join(", ", this.commandArguments.FilteredCategories) : "none")}",
                $"Included owners : {(this.commandArguments.IncludedOwners.Any() ? string.Join(", ", this.commandArguments.IncludedOwners) : "all")}",
                $"Excluded owners : {(this.commandArguments.ExcludedOwners.Any() ? string.Join(", ", this.commandArguments.ExcludedOwners) : "none")}",
                this.commandArguments.DryRun ? "Mode : DRY RUN (no changes persisted)" : "Mode : LIVE",
                string.Empty,
                "== Synchronised Element Usages ==",
            };

            lines.AddRange(this.reportEntries.Any() ? this.reportEntries : new List<string> { "(none)" });

            try
            {
                System.IO.File.WriteAllLines(fileName, lines);
                Console.WriteLine($"Element usage name sync report written to {Path.GetFullPath(fileName)}");
            }
            catch (IOException exception)
            {
                Console.WriteLine($"Failed to write element usage name sync report: {exception.Message}");
            }
        }
    }
}