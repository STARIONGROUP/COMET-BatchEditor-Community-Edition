//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SyncCommand.cs" company="Starion Group S.A.">
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
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Copies or updates <see cref="ElementDefinition" />s, their <see cref="Parameter" />s, parameter values and
    /// <see cref="ParameterGroup" />s from a source <see cref="EngineeringModel" /> into a target
    /// <see cref="EngineeringModel" />, matched by <see cref="ElementDefinition.ShortName" />.
    /// </summary>
    public class SyncCommand : ISyncCommand
    {
        /// <summary>
        /// The value that the model interprets as "no value set" (default) for a parameter value.
        /// </summary>
        private const string DefaultValue = "-";

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
        /// Per Element Definition lines describing what was copied or updated, written to the copy report.
        /// </summary>
        private readonly List<string> reportEntries = new();

        /// <summary>
        /// Notes (exclusions and skips) written to the copy report.
        /// </summary>
        private readonly List<string> reportNotes = new();

        /// <summary>
        /// Per value set lines describing the reference value changes, written to the copy report.
        /// </summary>
        private readonly List<string> valueChangeEntries = new();

        /// <summary>
        /// Short names of the element definitions that were pulled into the copy set only because a child element usage
        /// (member of an element usage category) referenced them.
        /// </summary>
        private readonly HashSet<string> elementUsageElementDefinitionShortNames = new();

        /// <summary>
        /// Short names of the element definitions that were actually changed during this run (created or updated, or
        /// involved in a created/updated element usage or parameter override). Used to suppress noise in the report.
        /// </summary>
        private readonly HashSet<string> changedElementDefinitionShortNames = new();

        /// <summary>
        /// The <see cref="ParameterType.Iid" />s reachable through the target model's chain of reference data libraries.
        /// </summary>
        private readonly HashSet<Guid> accessibleParameterTypeIids = new();

        /// <summary>
        /// The <see cref="Category.Iid" />s reachable through the target model's chain of reference data libraries.
        /// </summary>
        private readonly HashSet<Guid> accessibleCategoryIids = new();

        /// <summary>
        /// The parameters created during the structure phase whose values must be set after the structure has been
        /// persisted (the value sets of a newly created parameter are generated server-side).
        /// </summary>
        private readonly List<(string TargetElementShortName, Parameter SourceParameter)> deferredParameterValues = new();

        /// <summary>
        /// The parameter overrides created during the structure phase whose values must be set after the structure has
        /// been persisted (the value sets of a newly created override are generated server-side).
        /// </summary>
        private readonly List<(string PrimaryElementShortName, string UsageShortName, ParameterOverride SourceOverride)> deferredOverrideValues = new();

        /// <summary>
        /// The target <see cref="ElementDefinition" /> (created instance or update clone) and the transaction that holds
        /// it, keyed by source element definition short name. Used to attach element usages to the right container.
        /// </summary>
        private readonly Dictionary<string, ElementDefinitionUpdate> processedElementDefinitions = new();

        /// <summary>
        /// The parameters created during the structure phase, keyed by (target element short name, parameter type iid),
        /// so that overrides can reference them before they are persisted.
        /// </summary>
        private readonly Dictionary<(string ElementShortName, Guid ParameterTypeIid), Parameter> createdParameters = new();

        /// <summary>
        /// Initialise a new <see cref="SyncCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">the <see cref="ISessionService" /> providing the source and target iterations</param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public SyncCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Holds a target <see cref="ElementDefinition" /> (created instance or update clone) and lazily provides the
        /// transaction that carries its changes. Constructing a <see cref="ThingTransaction" /> registers its root as an
        /// updated thing, so for an existing element definition the transaction is only created (and added to the
        /// transactions to persist) when there is an actual change, avoiding no-op updates.
        /// </summary>
        private sealed class ElementDefinitionUpdate
        {
            private readonly Func<ThingTransaction> transactionFactory;
            private ThingTransaction transaction;

            /// <summary>
            /// Initialises an update with an already created (eager) transaction, used when the element definition is new.
            /// </summary>
            public ElementDefinitionUpdate(ElementDefinition target, ThingTransaction transaction)
            {
                this.Target = target;
                this.transaction = transaction;
            }

            /// <summary>
            /// Initialises an update with a factory that creates the transaction on first use, used for existing definitions.
            /// </summary>
            public ElementDefinitionUpdate(ElementDefinition target, Func<ThingTransaction> transactionFactory)
            {
                this.Target = target;
                this.transactionFactory = transactionFactory;
            }

            /// <summary>
            /// Gets the target element definition (created instance or update clone).
            /// </summary>
            public ElementDefinition Target { get; }

            /// <summary>
            /// Gets the transaction that holds the changes, creating it on first access for an existing definition.
            /// </summary>
            public ThingTransaction Transaction => this.transaction ??= this.transactionFactory();
        }

        /// <summary>
        /// Copies or updates the <see cref="ElementDefinition" />s of the source model into the target model and writes a
        /// copy report.
        /// </summary>
        public void Sync()
        {
            var source = this.sessionService.SourceIteration;
            var target = this.sessionService.TargetIteration;

            if (source == null || target == null)
            {
                Console.WriteLine("SyncElementDefinitions: source and/or target model could not be resolved. Aborting.");
                return;
            }

            this.BuildAccessibilitySets(target);

            var sourceDuplicateShortNames = DuplicateShortNames(source.Element);
            var targetDuplicateShortNames = DuplicateShortNames(target.Element);

            foreach (var shortName in sourceDuplicateShortNames.OrderBy(s => s))
            {
                this.reportNotes.Add($"[EXCLUDED] Element Definition short name '{shortName}' appears more than once in the source model and was skipped.");
            }

            foreach (var shortName in targetDuplicateShortNames.OrderBy(s => s))
            {
                this.reportNotes.Add($"[EXCLUDED] Element Definition short name '{shortName}' appears more than once in the target model and was skipped.");
            }

            var candidates = source.Element
                .Where(elementDefinition => !sourceDuplicateShortNames.Contains(elementDefinition.ShortName))
                .Where(elementDefinition => !targetDuplicateShortNames.Contains(elementDefinition.ShortName))
                .Where(elementDefinition => this.filterService.IsMemberOfSelectedCategory(elementDefinition))
                .OrderBy(elementDefinition => elementDefinition.ShortName)
                .ToList();

            var elementUsageCategoryShortNames = new HashSet<string>(this.commandArguments.ElementUsageCategories ?? Enumerable.Empty<string>());
            var qualifyingUsages = this.CollectQualifyingUsages(candidates, elementUsageCategoryShortNames);

            // Process the Element Definitions leaf-first so that, since each transaction is written as a separate
            // operation in list order, a usage (written in the transaction of its containing Element Definition) always
            // references an Element Definition that was already persisted by an earlier transaction.
            var elementDefinitionsToProcess = this.OrderElementDefinitionsToProcess(candidates, qualifyingUsages, sourceDuplicateShortNames, targetDuplicateShortNames);

            foreach (var sourceElementDefinition in elementDefinitionsToProcess)
            {
                var targetElementDefinition = target.Element.FirstOrDefault(elementDefinition => elementDefinition.ShortName == sourceElementDefinition.ShortName);

                if (targetElementDefinition == null)
                {
                    this.CreateElementDefinition(sourceElementDefinition, target);
                }
                else
                {
                    this.UpdateElementDefinition(sourceElementDefinition, targetElementDefinition);
                }
            }

            this.ProcessQualifyingUsages(qualifyingUsages);

            // Only note an element-usage-derived Element Definition when its pull-in actually changed something.
            foreach (var shortName in this.elementUsageElementDefinitionShortNames.OrderBy(shortName => shortName))
            {
                if (this.changedElementDefinitionShortNames.Contains(shortName))
                {
                    this.reportNotes.Add($"[ELEMENTUSAGE] '{shortName}' pulled into the copy set via a child element usage.");
                }
            }

            this.ApplyDeferredValues(target);

            this.WriteReport();
        }

        /// <summary>
        /// Collects, recursively, the child <see cref="ElementUsage" />s that are a member of at least one element usage
        /// category, starting from the primary candidate element definitions and descending into the element definitions
        /// that those usages reference. Membership uses the usage's effective categories (its own categories and super
        /// categories, combined with those of its referenced <see cref="ElementDefinition" />). Each pair records the
        /// usage together with the element definition that directly contains it, so nesting is preserved.
        /// </summary>
        /// <param name="candidates">The primary candidate source element definitions.</param>
        /// <param name="elementUsageCategoryShortNames">The element usage category short names.</param>
        /// <returns>The qualifying (containing source element definition, source usage) pairs.</returns>
        private List<(ElementDefinition PrimarySource, ElementUsage SourceUsage)> CollectQualifyingUsages(
            IEnumerable<ElementDefinition> candidates, HashSet<string> elementUsageCategoryShortNames)
        {
            var qualifyingUsages = new List<(ElementDefinition, ElementUsage)>();

            if (!elementUsageCategoryShortNames.Any())
            {
                return qualifyingUsages;
            }

            var scanned = new HashSet<string>();
            var toScan = new Queue<ElementDefinition>(candidates);

            while (toScan.Count > 0)
            {
                var elementDefinition = toScan.Dequeue();

                if (!scanned.Add(elementDefinition.ShortName))
                {
                    continue;
                }

                foreach (var usage in elementDefinition.ContainedElement.OrderBy(usage => usage.ShortName))
                {
                    var effectiveCategoryShortNames = usage.GetAllCategories(true).Select(category => category.ShortName)
                        .Concat(usage.ElementDefinition.GetAllCategories(true).Select(category => category.ShortName));

                    if (elementUsageCategoryShortNames.Overlaps(effectiveCategoryShortNames))
                    {
                        qualifyingUsages.Add((elementDefinition, usage));

                        // Descend into the referenced definition so its own qualifying usages are copied and nested under it.
                        toScan.Enqueue(usage.ElementDefinition);
                    }
                }
            }

            return qualifyingUsages;
        }

        /// <summary>
        /// Builds the ordered, de-duplicated list of source element definitions to process. The order is leaf-first
        /// (topological): an element definition that is referenced by a qualifying usage is processed before the element
        /// definition that contains that usage. Because each usage is written in the transaction of its containing
        /// element definition, this guarantees the referenced definition is persisted before the usage that points to it.
        /// Duplicates are skipped (already noted).
        /// </summary>
        /// <param name="candidates">The primary candidate source element definitions.</param>
        /// <param name="qualifyingUsages">The qualifying usages.</param>
        /// <param name="sourceDuplicateShortNames">The source duplicate short names.</param>
        /// <param name="targetDuplicateShortNames">The target duplicate short names.</param>
        /// <returns>The ordered source element definitions to process.</returns>
        private List<ElementDefinition> OrderElementDefinitionsToProcess(
            List<ElementDefinition> candidates,
            List<(ElementDefinition PrimarySource, ElementUsage SourceUsage)> qualifyingUsages,
            HashSet<string> sourceDuplicateShortNames,
            HashSet<string> targetDuplicateShortNames)
        {
            var candidateShortNames = new HashSet<string>(candidates.Select(elementDefinition => elementDefinition.ShortName));

            // All element definitions in scope (candidates plus the ones pulled in via a qualifying usage), keyed by short name.
            var inScope = new Dictionary<string, ElementDefinition>();

            void Consider(ElementDefinition elementDefinition)
            {
                if (elementDefinition != null
                    && !sourceDuplicateShortNames.Contains(elementDefinition.ShortName)
                    && !targetDuplicateShortNames.Contains(elementDefinition.ShortName))
                {
                    inScope.TryAdd(elementDefinition.ShortName, elementDefinition);
                }
            }

            foreach (var candidate in candidates)
            {
                Consider(candidate);
            }

            // Dependencies: a containing element definition depends on the element definition each of its qualifying usages references.
            var dependencies = new Dictionary<string, HashSet<string>>();

            foreach (var (primarySource, sourceUsage) in qualifyingUsages)
            {
                Consider(primarySource);
                Consider(sourceUsage.ElementDefinition);

                if (inScope.ContainsKey(primarySource.ShortName) && inScope.ContainsKey(sourceUsage.ElementDefinition.ShortName))
                {
                    if (!dependencies.TryGetValue(primarySource.ShortName, out var children))
                    {
                        children = new HashSet<string>();
                        dependencies[primarySource.ShortName] = children;
                    }

                    children.Add(sourceUsage.ElementDefinition.ShortName);
                }
            }

            var ordered = new List<ElementDefinition>();
            var visitState = new Dictionary<string, bool>();

            void Visit(string shortName)
            {
                // false = in progress (guards against cycles), true = completed.
                if (visitState.ContainsKey(shortName))
                {
                    return;
                }

                visitState[shortName] = false;

                if (dependencies.TryGetValue(shortName, out var children))
                {
                    foreach (var child in children.OrderBy(child => child))
                    {
                        Visit(child);
                    }
                }

                visitState[shortName] = true;
                ordered.Add(inScope[shortName]);

                if (!candidateShortNames.Contains(shortName))
                {
                    // Recorded now; the [ELEMENTUSAGE] note is only written later if this pull-in actually changed anything.
                    this.elementUsageElementDefinitionShortNames.Add(shortName);
                }
            }

            foreach (var shortName in inScope.Keys.OrderBy(shortName => shortName))
            {
                Visit(shortName);
            }

            return ordered;
        }

        /// <summary>
        /// Returns the set of short names that occur more than once in the given collection of element definitions.
        /// </summary>
        /// <param name="elementDefinitions">The element definitions to inspect.</param>
        /// <returns>The duplicated short names.</returns>
        private static HashSet<string> DuplicateShortNames(IEnumerable<ElementDefinition> elementDefinitions)
        {
            return elementDefinitions
                .GroupBy(elementDefinition => elementDefinition.ShortName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet();
        }

        /// <summary>
        /// Determines whether the given value array holds only the model's default value (a single "-" or empty value).
        /// </summary>
        /// <param name="valueArray">The value array to inspect.</param>
        /// <returns>True when the value array contains no meaningful value.</returns>
        private static bool IsDefaultValue(ValueArray<string> valueArray)
        {
            return valueArray == null || valueArray.All(value => string.IsNullOrEmpty(value) || value == DefaultValue);
        }

        /// <summary>
        /// Collects the <see cref="ParameterType" />s and <see cref="Category" />s reachable through the target model's
        /// chain of reference data libraries.
        /// </summary>
        /// <param name="target">The target <see cref="Iteration" />.</param>
        private void BuildAccessibilitySets(Iteration target)
        {
            var targetRdlChain = target.RequiredRdls.ToList();

            this.accessibleParameterTypeIids.Clear();
            this.accessibleParameterTypeIids.UnionWith(targetRdlChain.SelectMany(rdl => rdl.ParameterType).Select(parameterType => parameterType.Iid));

            this.accessibleCategoryIids.Clear();
            this.accessibleCategoryIids.UnionWith(targetRdlChain.SelectMany(rdl => rdl.DefinedCategory).Select(category => category.Iid));
        }

        /// <summary>
        /// Creates a new <see cref="ElementDefinition" /> in the target iteration from the given source element definition,
        /// together with its (accessible) categories, parameter groups and parameters.
        /// </summary>
        /// <param name="sourceElementDefinition">The source <see cref="ElementDefinition" /> to copy.</param>
        /// <param name="target">The target <see cref="Iteration" />.</param>
        private void CreateElementDefinition(ElementDefinition sourceElementDefinition, Iteration target)
        {
            var iterationClone = target.Clone(false);
            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(iterationClone), iterationClone);

            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
            {
                Name = sourceElementDefinition.Name,
                ShortName = sourceElementDefinition.ShortName,
                Owner = sourceElementDefinition.Owner
            };

            elementDefinition.Category.AddRange(this.AccessibleCategories(sourceElementDefinition, elementDefinition.ShortName));

            transaction.Create(elementDefinition, iterationClone);

            this.reportEntries.Add($"[CREATED] {elementDefinition.ShortName} ({elementDefinition.Name}) owner={elementDefinition.Owner?.ShortName}");
            Console.WriteLine($"Created Element Definition {elementDefinition.ShortName} in target model");

            var groupMap = this.CopyParameterGroups(sourceElementDefinition, elementDefinition, transaction);

            foreach (var sourceParameter in this.SelectableParameters(sourceElementDefinition))
            {
                this.CreateParameter(sourceParameter, elementDefinition, transaction, groupMap);
            }

            this.processedElementDefinitions[sourceElementDefinition.ShortName] = new ElementDefinitionUpdate(elementDefinition, transaction);
            this.changedElementDefinitionShortNames.Add(elementDefinition.ShortName);
            this.sessionService.Transactions.Add(transaction);
        }

        /// <summary>
        /// Updates an existing target <see cref="ElementDefinition" /> from the source: the name (when different), any
        /// missing accessible categories and any missing or value-outdated parameters. Nothing is written (and nothing is
        /// reported) when the target already matches the source.
        /// </summary>
        /// <param name="sourceElementDefinition">The source <see cref="ElementDefinition" />.</param>
        /// <param name="targetElementDefinition">The matching target <see cref="ElementDefinition" />.</param>
        private void UpdateElementDefinition(ElementDefinition sourceElementDefinition, ElementDefinition targetElementDefinition)
        {
            var elementDefinitionClone = targetElementDefinition.Clone(false);

            // The transaction is created lazily: constructing it registers the clone as an update, so it is only created
            // when there is an actual change to persist.
            var context = new ElementDefinitionUpdate(
                elementDefinitionClone,
                () =>
                {
                    var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(elementDefinitionClone), elementDefinitionClone);
                    this.sessionService.Transactions.Add(transaction);
                    return transaction;
                });

            this.processedElementDefinitions[sourceElementDefinition.ShortName] = context;

            var changes = new List<string>();

            if (elementDefinitionClone.Name != sourceElementDefinition.Name)
            {
                changes.Add($"name '{targetElementDefinition.Name}' -> '{sourceElementDefinition.Name}'");
                elementDefinitionClone.Name = sourceElementDefinition.Name;
                _ = context.Transaction;
            }

            foreach (var category in this.AccessibleCategories(sourceElementDefinition, targetElementDefinition.ShortName))
            {
                if (elementDefinitionClone.Category.All(existing => existing.Iid != category.Iid))
                {
                    elementDefinitionClone.Category.Add(category);
                    changes.Add($"+category {category.ShortName}");
                    _ = context.Transaction;
                }
            }

            Dictionary<Guid, ParameterGroup> groupMap = null;

            foreach (var sourceParameter in this.SelectableParameters(sourceElementDefinition))
            {
                var targetParameter = targetElementDefinition.Parameter.FirstOrDefault(parameter => parameter.ParameterType.Iid == sourceParameter.ParameterType.Iid);

                if (targetParameter == null)
                {
                    groupMap ??= this.CopyParameterGroups(sourceElementDefinition, elementDefinitionClone, context.Transaction);
                    this.CreateParameter(sourceParameter, elementDefinitionClone, context.Transaction, groupMap);
                    changes.Add($"+parameter {sourceParameter.ParameterType.ShortName}");
                }
                else if (this.UpdateExistingParameterValues(sourceParameter, targetParameter))
                {
                    changes.Add($"~parameter {sourceParameter.ParameterType.ShortName} (reference value)");
                }
            }

            if (changes.Any())
            {
                this.changedElementDefinitionShortNames.Add(targetElementDefinition.ShortName);
                this.reportEntries.Add($"[UPDATED] {targetElementDefinition.ShortName} ({sourceElementDefinition.Name}): {string.Join(", ", changes)}");
                Console.WriteLine($"Updated Element Definition {targetElementDefinition.ShortName} in target model");
            }
        }

        /// <summary>
        /// Returns the source element definition categories that are accessible through the target model's RDL chain,
        /// recording a note for any category that is not.
        /// </summary>
        /// <param name="sourceElementDefinition">The source <see cref="ElementDefinition" />.</param>
        /// <param name="targetElementShortName">The short name used in report notes.</param>
        /// <returns>The accessible <see cref="Category" />s.</returns>
        private IEnumerable<Category> AccessibleCategories(ElementDefinition sourceElementDefinition, string targetElementShortName)
        {
            foreach (var category in sourceElementDefinition.Category)
            {
                if (this.accessibleCategoryIids.Contains(category.Iid))
                {
                    yield return category;
                }
                else
                {
                    this.reportNotes.Add($"[SKIPPED] Category '{category.ShortName}' on '{targetElementShortName}' is not accessible through the target model's chain of reference data libraries.");
                }
            }
        }

        /// <summary>
        /// Returns the source parameters that pass the --parameters filter and whose parameter type is accessible through
        /// the target model's RDL chain, recording a note for any parameter type that is not.
        /// </summary>
        /// <param name="sourceElementDefinition">The source <see cref="ElementDefinition" />.</param>
        /// <returns>The selectable source <see cref="Parameter" />s.</returns>
        private IEnumerable<Parameter> SelectableParameters(ElementDefinition sourceElementDefinition)
        {
            foreach (var parameter in sourceElementDefinition.Parameter.OrderBy(parameter => parameter.ParameterType.ShortName))
            {
                if (!this.filterService.IsParameterSpecifiedOrAny(parameter))
                {
                    continue;
                }

                if (this.accessibleParameterTypeIids.Contains(parameter.ParameterType.Iid))
                {
                    yield return parameter;
                }
                else
                {
                    this.reportNotes.Add($"[SKIPPED] Parameter '{parameter.ParameterType.ShortName}' on '{sourceElementDefinition.ShortName}' is not accessible through the target model's chain of reference data libraries.");
                }
            }
        }

        /// <summary>
        /// Recreates (by name) the source parameter groups under the target element definition, preserving nesting, and
        /// returns a map from source <see cref="ParameterGroup.Iid" /> to the target <see cref="ParameterGroup" />.
        /// </summary>
        /// <param name="sourceElementDefinition">The source <see cref="ElementDefinition" />.</param>
        /// <param name="targetElementDefinition">The target <see cref="ElementDefinition" /> (clone or new instance).</param>
        /// <param name="transaction">The <see cref="ThingTransaction" /> holding the changes.</param>
        /// <returns>A map from source parameter group iid to the corresponding target parameter group.</returns>
        private Dictionary<Guid, ParameterGroup> CopyParameterGroups(ElementDefinition sourceElementDefinition, ElementDefinition targetElementDefinition, ThingTransaction transaction)
        {
            var map = new Dictionary<Guid, ParameterGroup>();

            // Order parents before children so a containing group exists before it is referenced.
            foreach (var sourceGroup in sourceElementDefinition.ParameterGroup.OrderBy(this.Depth))
            {
                var targetGroup = targetElementDefinition.ParameterGroup.FirstOrDefault(group => group.Name == sourceGroup.Name);

                if (targetGroup == null)
                {
                    targetGroup = new ParameterGroup(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri) { Name = sourceGroup.Name };
                    transaction.Create(targetGroup, targetElementDefinition);
                }

                if (sourceGroup.ContainingGroup != null && map.TryGetValue(sourceGroup.ContainingGroup.Iid, out var targetContainingGroup))
                {
                    targetGroup.ContainingGroup = targetContainingGroup;
                }

                map[sourceGroup.Iid] = targetGroup;
            }

            return map;
        }

        /// <summary>
        /// Computes the nesting depth of a parameter group (the number of containing groups above it).
        /// </summary>
        /// <param name="parameterGroup">The <see cref="ParameterGroup" />.</param>
        /// <returns>The nesting depth.</returns>
        private int Depth(ParameterGroup parameterGroup)
        {
            var depth = 0;

            for (var current = parameterGroup.ContainingGroup; current != null; current = current.ContainingGroup)
            {
                depth++;
            }

            return depth;
        }

        /// <summary>
        /// Creates a new <see cref="Parameter" /> under the given target element definition from the source parameter and
        /// schedules its value to be set after the structure has been persisted.
        /// </summary>
        /// <param name="sourceParameter">The source <see cref="Parameter" />.</param>
        /// <param name="targetElementDefinition">The target <see cref="ElementDefinition" /> (clone or new instance).</param>
        /// <param name="transaction">The <see cref="ThingTransaction" /> holding the changes.</param>
        /// <param name="groupMap">The map from source to target parameter groups.</param>
        private void CreateParameter(Parameter sourceParameter, ElementDefinition targetElementDefinition, ThingTransaction transaction, Dictionary<Guid, ParameterGroup> groupMap)
        {
            var parameter = new Parameter(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
            {
                ParameterType = sourceParameter.ParameterType,
                Scale = sourceParameter.Scale,
                Owner = sourceParameter.Owner
            };

            if (sourceParameter.Group != null && groupMap.TryGetValue(sourceParameter.Group.Iid, out var targetGroup))
            {
                parameter.Group = targetGroup;
            }

            transaction.Create(parameter, targetElementDefinition);
            this.createdParameters[(targetElementDefinition.ShortName, parameter.ParameterType.Iid)] = parameter;

            // The value sets of a newly created parameter are generated server-side; set their values after persisting.
            this.deferredParameterValues.Add((targetElementDefinition.ShortName, sourceParameter));
        }

        /// <summary>
        /// Sets the target parameter's reference value sets from the source parameter's published values. An existing
        /// value is only overwritten when the source published value is not the default ("-").
        /// </summary>
        /// <param name="sourceParameter">The source <see cref="Parameter" />.</param>
        /// <param name="targetParameter">The existing target <see cref="Parameter" />.</param>
        /// <returns>True when at least one value set was scheduled for update.</returns>
        private bool UpdateExistingParameterValues(Parameter sourceParameter, Parameter targetParameter)
        {
            var updated = false;

            foreach (var targetValueSet in targetParameter.ValueSet)
            {
                var sourceValueSet = MatchingSourceValueSet(sourceParameter, targetValueSet);

                if (sourceValueSet == null)
                {
                    this.reportNotes.Add($"[SKIPPED] Parameter '{sourceParameter.ParameterType.ShortName}' on '{(targetParameter.Container as ElementDefinition)?.ShortName}': no matching source value set (option/state structure differs).");
                    continue;
                }

                if (IsDefaultValue(sourceValueSet.Published))
                {
                    continue;
                }

                var oldReference = FormatValues(targetValueSet.Reference);

                if (oldReference == FormatValues(sourceValueSet.Published))
                {
                    // The reference value already matches the source published value: nothing to do.
                    continue;
                }

                // This is an update of an existing parameter: set the reference value but never change the value switch.
                var valueSetClone = targetValueSet.Clone(false);
                var valueTransaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                valueSetClone.Reference = new ValueArray<string>(sourceValueSet.Published);
                valueTransaction.CreateOrUpdate(valueSetClone);
                this.sessionService.Transactions.Add(valueTransaction);
                updated = true;

                var label = $"{(targetParameter.Container as ElementDefinition)?.ShortName}.{sourceParameter.ParameterType.ShortName}{Qualifier(targetValueSet.ActualOption?.ShortName, targetValueSet.ActualState?.ShortName)}";
                this.valueChangeEntries.Add($"[PARAMETER] {label}: reference '{oldReference}' -> '{FormatValues(sourceValueSet.Published)}'");
            }

            return updated;
        }

        /// <summary>
        /// Formats a value array for the report.
        /// </summary>
        /// <param name="values">The value array.</param>
        /// <returns>The pipe separated values, or an empty string.</returns>
        private static string FormatValues(ValueArray<string> values)
        {
            return values == null ? string.Empty : string.Join("|", values);
        }

        /// <summary>
        /// Builds an option/state qualifier suffix for a value set, or an empty string when neither applies.
        /// </summary>
        /// <param name="optionShortName">The actual option short name, if any.</param>
        /// <param name="stateShortName">The actual state short name, if any.</param>
        /// <returns>A qualifier such as " [option=o1, state=s1]", or an empty string.</returns>
        private static string Qualifier(string optionShortName, string stateShortName)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(optionShortName))
            {
                parts.Add($"option={optionShortName}");
            }

            if (!string.IsNullOrEmpty(stateShortName))
            {
                parts.Add($"state={stateShortName}");
            }

            return parts.Any() ? $" [{string.Join(", ", parts)}]" : string.Empty;
        }

        /// <summary>
        /// Finds the source <see cref="ParameterValueSet" /> matching the given target value set by actual option and
        /// actual state (by short name).
        /// </summary>
        /// <param name="sourceParameter">The source <see cref="Parameter" />.</param>
        /// <param name="targetValueSet">The target <see cref="ParameterValueSet" />.</param>
        /// <returns>The matching source <see cref="ParameterValueSet" />, or null.</returns>
        private static ParameterValueSet MatchingSourceValueSet(Parameter sourceParameter, ParameterValueSet targetValueSet)
        {
            return sourceParameter.ValueSet.FirstOrDefault(sourceValueSet =>
                sourceValueSet.ActualOption?.ShortName == targetValueSet.ActualOption?.ShortName
                && sourceValueSet.ActualState?.ShortName == targetValueSet.ActualState?.ShortName);
        }

        /// <summary>
        /// Processes the qualifying child usages: re-creates (or updates) each <see cref="ElementUsage" /> under the
        /// matching target element definition and copies its <see cref="ParameterOverride" />s.
        /// </summary>
        /// <param name="qualifyingUsages">The qualifying (primary source element definition, source usage) pairs.</param>
        private void ProcessQualifyingUsages(List<(ElementDefinition PrimarySource, ElementUsage SourceUsage)> qualifyingUsages)
        {
            foreach (var (primarySource, sourceUsage) in qualifyingUsages)
            {
                if (!this.processedElementDefinitions.TryGetValue(primarySource.ShortName, out var primaryContext))
                {
                    this.reportNotes.Add($"[SKIPPED] Element Usage '{sourceUsage.ShortName}' on '{primarySource.ShortName}': the containing Element Definition was excluded.");
                    continue;
                }

                if (!this.processedElementDefinitions.TryGetValue(sourceUsage.ElementDefinition.ShortName, out var referencedContext))
                {
                    this.reportNotes.Add($"[SKIPPED] Element Usage '{sourceUsage.ShortName}' on '{primarySource.ShortName}': referenced Element Definition '{sourceUsage.ElementDefinition.ShortName}' was excluded.");
                    continue;
                }

                this.CreateOrUpdateElementUsage(primaryContext, referencedContext, sourceUsage, sourceUsage.ElementDefinition.ShortName, primarySource.ShortName);
            }
        }

        /// <summary>
        /// Creates or updates the target <see cref="ElementUsage" /> for the given source usage and copies its overrides.
        /// </summary>
        /// <param name="primaryContext">The target primary element definition and its (lazy) transaction.</param>
        /// <param name="referencedContext">The target referenced element definition and its (lazy) transaction.</param>
        /// <param name="sourceUsage">The source <see cref="ElementUsage" />.</param>
        /// <param name="referencedShortName">The short name of the referenced element definition.</param>
        /// <param name="primaryShortName">The short name of the containing (primary) element definition.</param>
        private void CreateOrUpdateElementUsage(
            ElementDefinitionUpdate primaryContext,
            ElementDefinitionUpdate referencedContext,
            ElementUsage sourceUsage,
            string referencedShortName,
            string primaryShortName)
        {
            var existingUsage = primaryContext.Target.ContainedElement.FirstOrDefault(usage => usage.ShortName == sourceUsage.ShortName);
            ElementUsage targetUsage;

            // Track whether the usage (clone) has already been registered in the transaction so it is only registered
            // when it actually changes (name update or an override being added), never as a no-op re-registration.
            var usageRegistered = false;

            if (existingUsage == null)
            {
                targetUsage = new ElementUsage(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                {
                    Name = sourceUsage.Name,
                    ShortName = sourceUsage.ShortName,
                    Owner = sourceUsage.Owner,
                    ElementDefinition = referencedContext.Target
                };

                primaryContext.Transaction.Create(targetUsage, primaryContext.Target);
                usageRegistered = true;
                this.MarkChanged(primaryShortName, referencedShortName);
                this.reportEntries.Add($"[USAGE CREATED] {primaryShortName}.{targetUsage.ShortName} -> {referencedShortName} owner={targetUsage.Owner?.ShortName}");
                Console.WriteLine($"Created Element Usage {primaryShortName}.{targetUsage.ShortName}");
            }
            else
            {
                targetUsage = existingUsage.Clone(false);

                if (targetUsage.Name != sourceUsage.Name)
                {
                    // Update the name when different; never change the owner.
                    targetUsage.Name = sourceUsage.Name;
                    primaryContext.Transaction.CreateOrUpdate(targetUsage);
                    usageRegistered = true;
                    this.MarkChanged(primaryShortName, referencedShortName);
                    this.reportEntries.Add($"[USAGE UPDATED] {primaryShortName}.{targetUsage.ShortName} (name)");
                }
            }

            void EnsureUsageRegistered()
            {
                if (!usageRegistered)
                {
                    primaryContext.Transaction.CreateOrUpdate(targetUsage);
                    usageRegistered = true;
                }
            }

            foreach (var sourceOverride in sourceUsage.ParameterOverride)
            {
                this.CopyParameterOverride(primaryContext, referencedContext, targetUsage, existingUsage, sourceOverride, referencedShortName, primaryShortName, EnsureUsageRegistered);
            }
        }

        /// <summary>
        /// Creates or updates a <see cref="ParameterOverride" /> on the target usage from the source override, applying the
        /// same parameter filter, RDL accessibility, owner-on-create and value rules as parameters.
        /// </summary>
        /// <param name="primaryContext">The target primary element definition and its transaction.</param>
        /// <param name="referencedContext">The target referenced element definition and its transaction.</param>
        /// <param name="targetUsage">The target <see cref="ElementUsage" /> (new instance or update clone).</param>
        /// <param name="existingUsage">The existing target usage, or null when the usage is being created.</param>
        /// <param name="sourceOverride">The source <see cref="ParameterOverride" />.</param>
        /// <param name="referencedShortName">The short name of the referenced element definition.</param>
        /// <param name="primaryShortName">The short name of the containing (primary) element definition.</param>
        /// <param name="ensureUsageRegistered">Registers the (existing) target usage in the transaction on first need.</param>
        private void CopyParameterOverride(
            ElementDefinitionUpdate primaryContext,
            ElementDefinitionUpdate referencedContext,
            ElementUsage targetUsage,
            ElementUsage existingUsage,
            ParameterOverride sourceOverride,
            string referencedShortName,
            string primaryShortName,
            Action ensureUsageRegistered)
        {
            if (!this.filterService.IsParameterSpecifiedOrAny(sourceOverride.Parameter))
            {
                return;
            }

            var parameterTypeIid = sourceOverride.ParameterType.Iid;

            if (!this.accessibleParameterTypeIids.Contains(parameterTypeIid))
            {
                this.reportNotes.Add($"[SKIPPED] Parameter Override '{sourceOverride.ParameterType.ShortName}' on usage '{primaryShortName}.{targetUsage.ShortName}': parameter type not accessible through the target model's chain of reference data libraries.");
                return;
            }

            var targetParameter = this.ResolveTargetParameter(referencedShortName, referencedContext.Target, parameterTypeIid);

            if (targetParameter == null)
            {
                this.reportNotes.Add($"[SKIPPED] Parameter Override '{sourceOverride.ParameterType.ShortName}' on usage '{primaryShortName}.{targetUsage.ShortName}': overridden parameter not present on '{referencedShortName}'.");
                return;
            }

            var existingOverride = existingUsage?.ParameterOverride.FirstOrDefault(parameterOverride => parameterOverride.ParameterType.Iid == parameterTypeIid);

            if (existingOverride == null)
            {
                var parameterOverride = new ParameterOverride(Guid.NewGuid(), this.sessionService.Cache, this.commandArguments.ServerUri)
                {
                    Owner = sourceOverride.Owner,
                    Parameter = targetParameter
                };

                // Adding an override to an existing usage requires that usage to be part of the transaction.
                ensureUsageRegistered();
                primaryContext.Transaction.Create(parameterOverride, targetUsage);

                // The value sets of a newly created override are generated server-side; set their values after persisting.
                this.deferredOverrideValues.Add((primaryShortName, targetUsage.ShortName, sourceOverride));
                this.MarkChanged(primaryShortName, referencedShortName);
                this.reportEntries.Add($"[OVERRIDE CREATED] {primaryShortName}.{targetUsage.ShortName}.{sourceOverride.ParameterType.ShortName}");
            }
            else if (this.UpdateExistingOverrideValues(sourceOverride, existingOverride))
            {
                this.MarkChanged(primaryShortName, referencedShortName);
                this.reportEntries.Add($"[OVERRIDE UPDATED] {primaryShortName}.{targetUsage.ShortName}.{sourceOverride.ParameterType.ShortName} (reference value)");
            }
        }

        /// <summary>
        /// Records that the given element definitions were changed during this run.
        /// </summary>
        /// <param name="shortNames">The element definition short names to mark as changed.</param>
        private void MarkChanged(params string[] shortNames)
        {
            foreach (var shortName in shortNames)
            {
                if (!string.IsNullOrEmpty(shortName))
                {
                    this.changedElementDefinitionShortNames.Add(shortName);
                }
            }
        }

        /// <summary>
        /// Resolves the target <see cref="Parameter" /> for the given parameter type on the referenced element definition,
        /// preferring a parameter created during this run over an already existing one.
        /// </summary>
        /// <param name="referencedShortName">The short name of the referenced element definition.</param>
        /// <param name="referencedTarget">The target referenced element definition.</param>
        /// <param name="parameterTypeIid">The parameter type iid to resolve.</param>
        /// <returns>The resolved target <see cref="Parameter" />, or null.</returns>
        private Parameter ResolveTargetParameter(string referencedShortName, ElementDefinition referencedTarget, Guid parameterTypeIid)
        {
            if (this.createdParameters.TryGetValue((referencedShortName, parameterTypeIid), out var created))
            {
                return created;
            }

            return referencedTarget.Parameter.FirstOrDefault(parameter => parameter.ParameterType.Iid == parameterTypeIid);
        }

        /// <summary>
        /// Sets an existing override's reference value sets from the source override's published values, only overwriting
        /// when the source published value is not the default ("-") and never changing the value switch.
        /// </summary>
        /// <param name="sourceOverride">The source <see cref="ParameterOverride" />.</param>
        /// <param name="targetOverride">The existing target <see cref="ParameterOverride" />.</param>
        /// <returns>True when at least one value set was scheduled for update.</returns>
        private bool UpdateExistingOverrideValues(ParameterOverride sourceOverride, ParameterOverride targetOverride)
        {
            var updated = false;
            var targetUsage = targetOverride.Container as ElementUsage;
            var primaryShortName = (targetUsage?.Container as ElementDefinition)?.ShortName;

            foreach (var targetValueSet in targetOverride.ValueSet)
            {
                var sourceValueSet = MatchingSourceOverrideValueSet(sourceOverride, targetValueSet);

                if (sourceValueSet == null || IsDefaultValue(sourceValueSet.Published))
                {
                    continue;
                }

                var oldReference = FormatValues(targetValueSet.Reference);

                if (oldReference == FormatValues(sourceValueSet.Published))
                {
                    // The reference value already matches the source published value: nothing to do.
                    continue;
                }

                var valueSetClone = targetValueSet.Clone(false);
                var valueTransaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                valueSetClone.Reference = new ValueArray<string>(sourceValueSet.Published);
                valueTransaction.CreateOrUpdate(valueSetClone);
                this.sessionService.Transactions.Add(valueTransaction);
                updated = true;

                var label = $"{primaryShortName}.{targetUsage?.ShortName}.{sourceOverride.ParameterType.ShortName}{Qualifier(targetValueSet.ActualOption?.ShortName, targetValueSet.ActualState?.ShortName)}";
                this.valueChangeEntries.Add($"[OVERRIDE] {label}: reference '{oldReference}' -> '{FormatValues(sourceValueSet.Published)}'");
            }

            return updated;
        }

        /// <summary>
        /// Finds the source <see cref="ParameterOverrideValueSet" /> matching the given target value set by actual option
        /// and actual state (by short name).
        /// </summary>
        /// <param name="sourceOverride">The source <see cref="ParameterOverride" />.</param>
        /// <param name="targetValueSet">The target <see cref="ParameterOverrideValueSet" />.</param>
        /// <returns>The matching source <see cref="ParameterOverrideValueSet" />, or null.</returns>
        private static ParameterOverrideValueSet MatchingSourceOverrideValueSet(ParameterOverride sourceOverride, ParameterOverrideValueSet targetValueSet)
        {
            return sourceOverride.ValueSet.FirstOrDefault(sourceValueSet =>
                sourceValueSet.ActualOption?.ShortName == targetValueSet.ActualOption?.ShortName
                && sourceValueSet.ActualState?.ShortName == targetValueSet.ActualState?.ShortName);
        }

        /// <summary>
        /// After the structure has been persisted, sets the reference values of the newly created parameters and parameter
        /// overrides from the source published values. In a dry run the structure is not persisted, so this is reported
        /// but not applied.
        /// </summary>
        /// <param name="target">The target <see cref="Iteration" />.</param>
        private void ApplyDeferredValues(Iteration target)
        {
            if (!this.deferredParameterValues.Any() && !this.deferredOverrideValues.Any())
            {
                return;
            }

            if (this.commandArguments.DryRun)
            {
                if (this.deferredParameterValues.Any())
                {
                    this.reportNotes.Add($"[INFO] {this.deferredParameterValues.Count} newly created parameter(s) will receive their reference values on a non-dry run (value sets are generated server-side).");
                }

                if (this.deferredOverrideValues.Any())
                {
                    this.reportNotes.Add($"[INFO] {this.deferredOverrideValues.Count} newly created parameter override(s) will receive their reference values on a non-dry run (value sets are generated server-side).");
                }

                return;
            }

            // Persist the structure first so the server-generated value sets become available.
            this.sessionService.Save();
            this.sessionService.Transactions.Clear();

            foreach (var (targetElementShortName, sourceParameter) in this.deferredParameterValues)
            {
                var targetElementDefinition = target.Element.FirstOrDefault(elementDefinition => elementDefinition.ShortName == targetElementShortName);
                var targetParameter = targetElementDefinition?.Parameter.FirstOrDefault(parameter => parameter.ParameterType.Iid == sourceParameter.ParameterType.Iid);

                if (targetParameter == null)
                {
                    this.reportNotes.Add($"[SKIPPED] Could not resolve newly created parameter '{sourceParameter.ParameterType.ShortName}' on '{targetElementShortName}' to set its reference value.");
                    continue;
                }

                foreach (var targetValueSet in targetParameter.ValueSet)
                {
                    var sourceValueSet = MatchingSourceValueSet(sourceParameter, targetValueSet);

                    if (sourceValueSet == null)
                    {
                        continue;
                    }

                    // This parameter was just created, so it is the first copy: set the reference value and switch the
                    // value to REFERENCE so the copied value becomes the active one. The switch is set even when the
                    // published value is the default ("-").
                    var valueSetClone = targetValueSet.Clone(false);
                    var valueTransaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                    valueSetClone.Reference = new ValueArray<string>(sourceValueSet.Published);
                    valueSetClone.ValueSwitch = ParameterSwitchKind.REFERENCE;
                    valueTransaction.CreateOrUpdate(valueSetClone);
                    this.sessionService.Transactions.Add(valueTransaction);

                    var label = $"{targetElementShortName}.{sourceParameter.ParameterType.ShortName}{Qualifier(targetValueSet.ActualOption?.ShortName, targetValueSet.ActualState?.ShortName)}";
                    this.valueChangeEntries.Add($"[PARAMETER] {label}: reference set to '{FormatValues(sourceValueSet.Published)}' (new, switch REFERENCE)");
                }
            }

            foreach (var (primaryElementShortName, usageShortName, sourceOverride) in this.deferredOverrideValues)
            {
                var primaryElementDefinition = target.Element.FirstOrDefault(elementDefinition => elementDefinition.ShortName == primaryElementShortName);
                var targetUsage = primaryElementDefinition?.ContainedElement.FirstOrDefault(usage => usage.ShortName == usageShortName);
                var targetOverride = targetUsage?.ParameterOverride.FirstOrDefault(parameterOverride => parameterOverride.ParameterType.Iid == sourceOverride.ParameterType.Iid);

                if (targetOverride == null)
                {
                    this.reportNotes.Add($"[SKIPPED] Could not resolve newly created override '{sourceOverride.ParameterType.ShortName}' on usage '{primaryElementShortName}.{usageShortName}' to set its reference value.");
                    continue;
                }

                foreach (var targetValueSet in targetOverride.ValueSet)
                {
                    var sourceValueSet = MatchingSourceOverrideValueSet(sourceOverride, targetValueSet);

                    if (sourceValueSet == null)
                    {
                        continue;
                    }

                    // This override was just created, so it is the first copy: set the reference value and switch to
                    // REFERENCE. The switch is set even when the published value is the default ("-").
                    var valueSetClone = targetValueSet.Clone(false);
                    var valueTransaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                    valueSetClone.Reference = new ValueArray<string>(sourceValueSet.Published);
                    valueSetClone.ValueSwitch = ParameterSwitchKind.REFERENCE;
                    valueTransaction.CreateOrUpdate(valueSetClone);
                    this.sessionService.Transactions.Add(valueTransaction);

                    var label = $"{primaryElementShortName}.{usageShortName}.{sourceOverride.ParameterType.ShortName}{Qualifier(targetValueSet.ActualOption?.ShortName, targetValueSet.ActualState?.ShortName)}";
                    this.valueChangeEntries.Add($"[OVERRIDE] {label}: reference set to '{FormatValues(sourceValueSet.Published)}' (new, switch REFERENCE)");
                }
            }
        }

        /// <summary>
        /// Writes the copy report to a LastSyncReport.txt file in the current working directory, overwriting any
        /// existing file.
        /// </summary>
        private void WriteReport()
        {
            const string fileName = "LastSyncReport.txt";

            var lines = new List<string>
            {
                $"Last Sync Report - generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Source model : {this.commandArguments.SourceModel}",
                $"Target model : {this.commandArguments.TargetModel}",
                $"Categories filter : {(this.commandArguments.FilteredCategories.Any() ? string.Join(", ", this.commandArguments.FilteredCategories) : "none")}",
                $"Element usage categories : {(this.commandArguments.ElementUsageCategories.Any() ? string.Join(", ", this.commandArguments.ElementUsageCategories) : "none")}",
                $"Parameters filter : {(this.commandArguments.SelectedParameters.Any() ? string.Join(", ", this.commandArguments.SelectedParameters) : "none")}",
                this.commandArguments.DryRun ? "Mode : DRY RUN (no changes persisted)" : "Mode : LIVE",
                string.Empty,
                "== Copied / Updated Element Definitions ==",
            };

            lines.AddRange(this.reportEntries.Any() ? this.reportEntries : new List<string> { "(none)" });
            lines.Add(string.Empty);
            lines.Add("== Parameter Value Changes ==");
            lines.AddRange(this.valueChangeEntries.Any() ? this.valueChangeEntries : new List<string> { "(none)" });
            lines.Add(string.Empty);
            lines.Add("== Notes ==");
            lines.AddRange(this.reportNotes.Any() ? this.reportNotes : new List<string> { "(none)" });

            try
            {
                System.IO.File.WriteAllLines(fileName, lines);
                Console.WriteLine($"Copy report written to {Path.GetFullPath(fileName)}");
            }
            catch (IOException exception)
            {
                Console.WriteLine($"Failed to write copy report: {exception.Message}");
            }
        }
    }
}
