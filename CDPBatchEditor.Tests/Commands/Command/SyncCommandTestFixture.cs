//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SyncCommandTestFixture.cs" company="Starion Group S.A.">
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

namespace CDPBatchEditor.Tests.Commands.Command
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using File = System.IO.File;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command;
    using CDPBatchEditor.Services.Interfaces;

    using CommandLine;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class SyncCommandTestFixture
    {
        private const string BaseUri = "http://test.com";

        private Uri uri;
        private Assembler assembler;
        private Mock<ICDPMessageBus> messageBus;
        private SiteDirectory siteDirectory;
        private DomainOfExpertise sys;

        private TextParameterType massParameterType;
        private TextParameterType powerParameterType;
        private TextParameterType secretParameterType;
        private Category equipmentCategory;
        private Category inaccessibleCategory;
        private Category batteryCategory;

        private Iteration sourceIteration;
        private Iteration targetIteration;

        private ICommandArguments commandArguments;
        private Mock<ISessionService> sessionService;
        private Mock<IFilterService> filterService;
        private List<ThingTransaction> transactions;
        private SyncCommand syncCommand;

        [SetUp]
        public void Setup()
        {
            this.uri = new Uri(BaseUri);
            this.messageBus = new Mock<ICDPMessageBus>();
            this.assembler = new Assembler(this.uri, this.messageBus.Object);
            this.transactions = new List<ThingTransaction>();

            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.sys = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "System", ShortName = "sys" };
            this.siteDirectory.Domain.Add(this.sys);

            this.SetupReferenceData();
            this.SetupSourceModel();
            this.SetupTargetModel();
            this.SetupMocks();
        }

        [TearDown]
        public void TearDown()
        {
            var reportFile = Path.Combine(Directory.GetCurrentDirectory(), "LastSyncReport.txt");

            try
            {
                if (File.Exists(reportFile))
                {
                    File.Delete(reportFile);
                }
            }
            catch (IOException)
            {
                // best effort clean up
            }
        }

        [Test]
        public void VerifyCreatesMissingElementDefinitionWithAccessibleCategoriesAndParameters()
        {
            this.BuildAndRun(string.Empty);

            var createdElementDefinition = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementDefinition>().FirstOrDefault(e => e.ShortName == "newEd");

            Assert.That(createdElementDefinition, Is.Not.Null);
            Assert.That(createdElementDefinition.Owner, Is.EqualTo(this.sys));
            Assert.That(createdElementDefinition.Category.Select(c => c.ShortName), Does.Contain("eq"));
            Assert.That(createdElementDefinition.Category.Select(c => c.ShortName), Does.Not.Contain("cat_inacc"));

            var createdParameters = this.transactions.SelectMany(t => t.AddedThing).OfType<Parameter>().ToList();
            Assert.That(createdParameters.Any(p => p.ParameterType.ShortName == "mass"), Is.True);
            Assert.That(createdParameters.Any(p => p.ParameterType.ShortName == "secret"), Is.False, "ParameterType not in the target RDL chain must be skipped.");
        }

        [Test]
        public void VerifyUpdatesNameAndReferenceValueOfExistingElementDefinition()
        {
            this.BuildAndRun(string.Empty);

            var updatedElementDefinition = this.transactions.SelectMany(t => t.UpdatedThing.Values).OfType<ElementDefinition>().FirstOrDefault(e => e.ShortName == "existing");

            Assert.That(updatedElementDefinition, Is.Not.Null);
            Assert.That(updatedElementDefinition.Name, Is.EqualTo("Existing NEW"));

            var updatedValueSets = this.transactions.SelectMany(t => t.UpdatedThing.Values).OfType<ParameterValueSet>().ToList();

            Assert.That(updatedValueSets.Count, Is.EqualTo(1), "Only the parameter whose source published value is not '-' should be updated.");
            Assert.That(updatedValueSets[0].Reference.First(), Is.EqualTo("50"));
            Assert.That(updatedValueSets[0].ValueSwitch, Is.EqualTo(ParameterSwitchKind.MANUAL), "An update must not change the existing value switch.");
        }

        [Test]
        public void VerifyReportRecordsExclusionsAndSkips()
        {
            this.BuildAndRun(string.Empty);

            var report = this.ReadReport();

            Assert.That(report, Does.Contain("newEd"));
            Assert.That(report, Does.Contain("existing"));
            Assert.That(report, Does.Contain("dupSrc").And.Contain("source"));
            Assert.That(report, Does.Contain("dupTgt").And.Contain("target"));
            Assert.That(report, Does.Contain("secret"));
            Assert.That(report, Does.Contain("cat_inacc"));
        }

        [Test]
        public void VerifyUnchangedElementDefinitionIsNotUpdated()
        {
            this.BuildAndRun(string.Empty);

            // "stable" matches the source exactly, so it must not be written or reported as a change.
            var touched = this.transactions
                .SelectMany(t => t.AddedThing.Concat(t.UpdatedThing.Values))
                .OfType<ElementDefinition>()
                .Select(e => e.ShortName);

            Assert.That(touched, Does.Not.Contain("stable"));
            Assert.That(this.ReadReport(), Does.Not.Contain("stable"));
        }

        [Test]
        public void VerifyNestedGroupsAreFlattenedToTopLevelOnCreate()
        {
            this.BuildAndRun(string.Empty);

            var createdGroups = this.transactions.SelectMany(t => t.AddedThing).OfType<ParameterGroup>().ToList();

            var gA = createdGroups.FirstOrDefault(g => g.Name == "gA");
            Assert.That(gA, Is.Not.Null);
            Assert.That(gA.ContainingGroup, Is.Null, "The copied group must be created at the top level.");
            Assert.That(createdGroups.Any(g => g.Name == "gB" || g.Name == "gC"), Is.False, "Nested sub-groups must not be copied.");
            Assert.That(createdGroups.Any(g => g.Name == "gUnused"), Is.False, "Unused groups must not be copied.");

            var createdMass = this.transactions.SelectMany(t => t.AddedThing).OfType<Parameter>()
                .FirstOrDefault(p => p.ParameterType.ShortName == "mass" && (p.Container as ElementDefinition)?.ShortName == "grp");

            Assert.That(createdMass, Is.Not.Null);
            Assert.That(createdMass.Group?.Name, Is.EqualTo("gA"), "A deeply nested parameter must be placed in the top-level group.");
        }

        [Test]
        public void VerifyNestedSourceGroupsFlattenedWhenRelinkingExistingParameters()
        {
            this.BuildAndRun(string.Empty);

            // Only the top-level "fA" group is created for grpFlat (no nesting).
            var createdGroups = this.transactions.SelectMany(t => t.AddedThing).OfType<ParameterGroup>()
                .Where(g => (g.Container as ElementDefinition)?.ShortName == "grpFlat").ToList();

            Assert.That(createdGroups.Select(g => g.Name), Is.EquivalentTo(new[] { "fA" }));
            Assert.That(createdGroups[0].ContainingGroup, Is.Null);

            // Both existing parameters (one from a deeply nested source group) are re-linked to the top-level "fA".
            var updated = this.transactions.SelectMany(t => t.UpdatedThing.Values).OfType<Parameter>()
                .Where(p => (p.Container as ElementDefinition)?.ShortName == "grpFlat").ToList();

            Assert.That(updated.FirstOrDefault(p => p.ParameterType.ShortName == "mass")?.Group?.Name, Is.EqualTo("fA"));
            Assert.That(updated.FirstOrDefault(p => p.ParameterType.ShortName == "power")?.Group?.Name, Is.EqualTo("fA"));
        }

        [Test]
        public void VerifyParameterGroupsRelinkedAndEmptyGroupDeletedOnUpdate()
        {
            this.BuildAndRun("--prune-groups");

            // "Old" exists only in the target and becomes empty after re-linking, so with --prune-groups it is deleted.
            var deletedGroups = this.transactions.SelectMany(t => t.DeletedThing).OfType<ParameterGroup>().Select(g => g.Name).ToList();
            Assert.That(deletedGroups, Does.Contain("Old"));

            var updatedParameters = this.transactions.SelectMany(t => t.UpdatedThing.Values).OfType<Parameter>()
                .Where(p => (p.Container as ElementDefinition)?.ShortName == "grpUpd").ToList();

            var mass = updatedParameters.FirstOrDefault(p => p.ParameterType.ShortName == "mass");
            Assert.That(mass, Is.Not.Null);
            Assert.That(mass.Group?.Name, Is.EqualTo("B"), "The moved parameter must be re-linked to the source group.");

            var power = updatedParameters.FirstOrDefault(p => p.ParameterType.ShortName == "power");
            Assert.That(power, Is.Not.Null);
            Assert.That(power.Group, Is.Null, "An ungrouped source parameter must clear the target group link.");
        }

        [Test]
        public void VerifyEmptyGroupIsKeptWithoutPruneOptionButParametersStillRelinked()
        {
            this.BuildAndRun(string.Empty);

            // Without --prune-groups the empty target-only group must be left in place.
            var deletedGroups = this.transactions.SelectMany(t => t.DeletedThing).OfType<ParameterGroup>().Select(g => g.Name).ToList();
            Assert.That(deletedGroups, Does.Not.Contain("Old"));

            // Re-linking still happens regardless of the prune option.
            var mass = this.transactions.SelectMany(t => t.UpdatedThing.Values).OfType<Parameter>()
                .FirstOrDefault(p => p.ParameterType.ShortName == "mass" && (p.Container as ElementDefinition)?.ShortName == "grpUpd");

            Assert.That(mass, Is.Not.Null);
            Assert.That(mass.Group?.Name, Is.EqualTo("B"));
        }

        [Test]
        public void VerifyReportRecordsParameterValueChanges()
        {
            this.BuildAndRun(string.Empty);

            var report = this.ReadReport();

            // The existing "existing" element definition's mass reference value changes from "1" to "50".
            Assert.That(report, Does.Contain("== Parameter Value Changes =="));
            Assert.That(report, Does.Contain("existing.mass").And.Contain("'1' -> '50'"));
        }

        [Test]
        public void VerifyCategoryFilterRestrictsCopiedElementDefinitions()
        {
            this.BuildAndRun("--categories eq");

            // "existing" is not a member of the "eq" category, so it must not be touched.
            var touchedShortNames = this.transactions
                .SelectMany(t => t.AddedThing.Concat(t.UpdatedThing.Values))
                .OfType<ElementDefinition>()
                .Select(e => e.ShortName)
                .ToList();

            Assert.That(touchedShortNames, Does.Contain("newEd"));
            Assert.That(touchedShortNames, Does.Not.Contain("existing"));
        }

        [Test]
        public void VerifyElementUsageCategoryPullsInReferencedElementDefinitions()
        {
            this.BuildAndRun("--categories eq --element-usage-categories battery");

            var createdElementDefinitions = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementDefinition>().Select(e => e.ShortName).ToList();

            // edA qualifies via the usage's own category, edB via the referenced ElementDefinition's category.
            Assert.That(createdElementDefinitions, Does.Contain("edA"));
            Assert.That(createdElementDefinitions, Does.Contain("edB"));

            var report = this.ReadReport();
            Assert.That(report, Does.Contain("[ELEMENTUSAGE]").And.Contain("edA"));
            Assert.That(report, Does.Contain("edB"));
        }

        [Test]
        public void VerifyElementUsagesAndOverridesAreCopied()
        {
            this.BuildAndRun("--categories eq --element-usage-categories battery");

            var createdUsages = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementUsage>().ToList();

            var usageA = createdUsages.FirstOrDefault(u => u.ShortName == "uA");
            Assert.That(usageA, Is.Not.Null);
            Assert.That(usageA.ElementDefinition.ShortName, Is.EqualTo("edA"));
            Assert.That(usageA.Owner, Is.EqualTo(this.sys));

            Assert.That(createdUsages.Any(u => u.ShortName == "uB" && u.ElementDefinition.ShortName == "edB"), Is.True);

            var createdOverride = this.transactions.SelectMany(t => t.AddedThing).OfType<ParameterOverride>().FirstOrDefault();
            Assert.That(createdOverride, Is.Not.Null);
            Assert.That(createdOverride.ParameterType.ShortName, Is.EqualTo("mass"));
            Assert.That(createdOverride.Owner, Is.EqualTo(this.sys));
        }

        [Test]
        public void VerifyUsagesAreNotPulledWithoutElementUsageCategories()
        {
            this.BuildAndRun("--categories eq");

            Assert.That(this.transactions.SelectMany(t => t.AddedThing).OfType<ElementUsage>(), Is.Empty);
            Assert.That(this.transactions.SelectMany(t => t.AddedThing).OfType<ElementDefinition>().Select(e => e.ShortName), Does.Not.Contain("edA").And.Not.Contain("edB"));
        }

        [Test]
        public void VerifyElementUsageNoteSkippedWhenUsageAndDefinitionUnchanged()
        {
            this.BuildAndRun("--categories eq --element-usage-categories battery");

            var report = this.ReadReport();

            // "stableChild" is pulled in via an existing, unchanged usage, so it must not be reported (no [ELEMENTUSAGE] note, no change).
            Assert.That(report, Does.Not.Contain("stableChild"));

            // The mechanism still fires for definitions that did change (edTreeC is created via the recursive usage chain).
            Assert.That(report, Does.Contain("[ELEMENTUSAGE]").And.Contain("edTreeC"));
        }

        [Test]
        public void VerifyNestedUsageIsAttachedToCorrectParent()
        {
            this.BuildAndRun("--element-usage-categories battery");

            var createdUsages = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementUsage>().ToList();

            var usageToB = createdUsages.FirstOrDefault(u => u.ElementDefinition.ShortName == "edTreeB");
            Assert.That(usageToB, Is.Not.Null);
            Assert.That((usageToB.Container as ElementDefinition)?.ShortName, Is.EqualTo("edTreeA"));

            var usageToC = createdUsages.FirstOrDefault(u => u.ElementDefinition.ShortName == "edTreeC");
            Assert.That(usageToC, Is.Not.Null);
            Assert.That((usageToC.Container as ElementDefinition)?.ShortName, Is.EqualTo("edTreeB"), "The usage based on C must be nested under B, not A.");

            // The element definitions must be created leaf-first so a usage is always written after the definition it references.
            var createdOrder = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementDefinition>().Select(e => e.ShortName).ToList();
            Assert.That(createdOrder.IndexOf("edTreeC"), Is.LessThan(createdOrder.IndexOf("edTreeB")));
            Assert.That(createdOrder.IndexOf("edTreeB"), Is.LessThan(createdOrder.IndexOf("edTreeA")));
        }

        [Test]
        public void VerifyNestedUsagesAreRecursivelyPulledWithCategoryFilter()
        {
            // Only edTreeA is a primary candidate (eq); edTreeB and edTreeC must still be reached through the usages.
            this.BuildAndRun("--categories eq --element-usage-categories battery");

            var created = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementDefinition>().Select(e => e.ShortName).ToList();
            Assert.That(created, Does.Contain("edTreeB"));
            Assert.That(created, Does.Contain("edTreeC"));

            var usageToC = this.transactions.SelectMany(t => t.AddedThing).OfType<ElementUsage>().FirstOrDefault(u => u.ElementDefinition.ShortName == "edTreeC");
            Assert.That(usageToC, Is.Not.Null);
            Assert.That((usageToC.Container as ElementDefinition)?.ShortName, Is.EqualTo("edTreeB"), "The usage based on C must be nested under B, not A.");
        }

        private void SetupReferenceData()
        {
            this.massParameterType = new TextParameterType(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "mass", Name = "mass" };
            this.powerParameterType = new TextParameterType(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "power", Name = "power" };
            this.secretParameterType = new TextParameterType(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "secret", Name = "secret" };
            this.equipmentCategory = new Category(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "eq", Name = "Equipment" };
            this.inaccessibleCategory = new Category(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "cat_inacc", Name = "Inaccessible" };
            this.batteryCategory = new Category(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "battery", Name = "Battery" };
        }

        private void SetupSourceModel()
        {
            var sourceSiteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri);
            var sourceMrdl = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri) { RequiredRdl = sourceSiteRdl };
            var sourceModel = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, this.uri);
            var sourceModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "SRC", ShortName = "SRC" };
            sourceModelSetup.RequiredRdl.Add(sourceMrdl);
            sourceModel.EngineeringModelSetup = sourceModelSetup;
            this.siteDirectory.Model.Add(sourceModelSetup);

            var sourceIterationSetup = new IterationSetup(Guid.NewGuid(), this.assembler.Cache, this.uri);
            sourceModelSetup.IterationSetup.Add(sourceIterationSetup);

            this.sourceIteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri) { IterationSetup = sourceIterationSetup };
            sourceModel.Iteration.Add(this.sourceIteration);

            var newEd = this.CreateElementDefinition("newEd", "New Eq", this.sourceIteration, this.equipmentCategory, this.inaccessibleCategory);
            this.AddParameter(newEd, this.massParameterType, "12");
            this.AddParameter(newEd, this.secretParameterType, "9");

            var existing = this.CreateElementDefinition("existing", "Existing NEW", this.sourceIteration);
            this.AddParameter(existing, this.massParameterType, "50");
            this.AddParameter(existing, this.powerParameterType, "-");

            // "stable" is identical in source and target (same name, same published reference value): nothing to do.
            var stable = this.CreateElementDefinition("stable", "Stable", this.sourceIteration);
            this.AddParameter(stable, this.massParameterType, "5");

            this.CreateElementDefinition("dupSrc", "Dup Source A", this.sourceIteration);
            this.CreateElementDefinition("dupSrc", "Dup Source B", this.sourceIteration);

            this.CreateElementDefinition("dupTgt", "Dup Target Source", this.sourceIteration);

            // Child element definitions reachable only through a element-usage-category usage of "newEd".
            // edA qualifies because the usage itself carries the battery category; edB because the referenced ED does.
            var edA = this.CreateElementDefinition("edA", "Battery A", this.sourceIteration);
            var edAMass = this.AddParameter(edA, this.massParameterType, "7");

            var edB = this.CreateElementDefinition("edB", "Battery B", this.sourceIteration, this.batteryCategory);
            this.AddParameter(edB, this.powerParameterType, "9");

            var usageA = this.AddUsage(newEd, edA, "uA", this.batteryCategory);
            this.AddOverride(usageA, edAMass, "8");

            this.AddUsage(newEd, edB, "uB");

            // A nested decomposition: edTreeA -> (usage of) edTreeB -> (usage of) edTreeC.
            var edTreeC = this.CreateElementDefinition("edTreeC", "Tree C", this.sourceIteration);
            this.AddParameter(edTreeC, this.massParameterType, "3");
            var edTreeB = this.CreateElementDefinition("edTreeB", "Tree B", this.sourceIteration);
            var edTreeA = this.CreateElementDefinition("edTreeA", "Tree A", this.sourceIteration, this.equipmentCategory);
            this.AddUsage(edTreeA, edTreeB, "uTreeB", this.batteryCategory);
            this.AddUsage(edTreeB, edTreeC, "uTreeC", this.batteryCategory);

            // A element-usage-category usage that already exists unchanged in the target: must not be reported at all.
            var stableChild = this.CreateElementDefinition("stableChild", "Stable Child", this.sourceIteration);
            this.AddParameter(stableChild, this.massParameterType, "5");
            var stableParent = this.CreateElementDefinition("stableParent", "Stable Parent", this.sourceIteration, this.equipmentCategory);
            this.AddUsage(stableParent, stableChild, "uStable", this.batteryCategory);

            // Nested groups are flattened: only the top-level (root) group is copied. gC -> gB -> gA; gUnused is unused.
            var grp = this.CreateElementDefinition("grp", "Grp", this.sourceIteration);
            var gA = this.AddParameterGroup(grp, "gA", null);
            var gB = this.AddParameterGroup(grp, "gB", gA);
            var gC = this.AddParameterGroup(grp, "gC", gB);
            this.AddParameterGroup(grp, "gUnused", null);
            this.AddParameter(grp, this.massParameterType, "1").Group = gC;

            // Flatten on update: source params are nested fA/fB/fC; target params exist ungrouped -> all land in fA.
            var grpFlatSource = this.CreateElementDefinition("grpFlat", "GrpFlat", this.sourceIteration);
            var fA = this.AddParameterGroup(grpFlatSource, "fA", null);
            var fB = this.AddParameterGroup(grpFlatSource, "fB", fA);
            var fC = this.AddParameterGroup(grpFlatSource, "fC", fB);
            this.AddParameter(grpFlatSource, this.massParameterType, "1").Group = fA;
            this.AddParameter(grpFlatSource, this.powerParameterType, "2").Group = fC;

            // Group re-link and empty-group deletion on update: source has top-level group "B" only.
            var grpUpdSource = this.CreateElementDefinition("grpUpd", "GrpUpd", this.sourceIteration);
            var sourceB = this.AddParameterGroup(grpUpdSource, "B", null);
            this.AddParameter(grpUpdSource, this.massParameterType, "1").Group = sourceB;
            this.AddParameter(grpUpdSource, this.powerParameterType, "2");

            this.AddToCache(this.sourceIteration);
        }

        private void SetupTargetModel()
        {
            var targetSiteRdl = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri);
            targetSiteRdl.ParameterType.Add(this.massParameterType);
            targetSiteRdl.ParameterType.Add(this.powerParameterType);
            targetSiteRdl.DefinedCategory.Add(this.equipmentCategory);
            targetSiteRdl.DefinedCategory.Add(this.batteryCategory);
            this.siteDirectory.SiteReferenceDataLibrary.Add(targetSiteRdl);

            var targetMrdl = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, this.uri) { RequiredRdl = targetSiteRdl };
            var targetModel = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, this.uri);
            var targetModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "TGT", ShortName = "TGT" };
            targetModelSetup.RequiredRdl.Add(targetMrdl);
            targetModel.EngineeringModelSetup = targetModelSetup;
            this.siteDirectory.Model.Add(targetModelSetup);

            var targetIterationSetup = new IterationSetup(Guid.NewGuid(), this.assembler.Cache, this.uri);
            targetModelSetup.IterationSetup.Add(targetIterationSetup);

            this.targetIteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri) { IterationSetup = targetIterationSetup };
            targetModel.Iteration.Add(this.targetIteration);

            var existing = this.CreateElementDefinition("existing", "Existing OLD", this.targetIteration);
            this.AddParameter(existing, this.massParameterType, "1", "1", ParameterSwitchKind.MANUAL);
            this.AddParameter(existing, this.powerParameterType, "-", "-");

            var stable = this.CreateElementDefinition("stable", "Stable", this.targetIteration);
            this.AddParameter(stable, this.massParameterType, "5", "5");

            var stableChild = this.CreateElementDefinition("stableChild", "Stable Child", this.targetIteration);
            this.AddParameter(stableChild, this.massParameterType, "5", "5");
            var stableParent = this.CreateElementDefinition("stableParent", "Stable Parent", this.targetIteration, this.equipmentCategory);
            this.AddUsage(stableParent, stableChild, "uStable", this.batteryCategory);

            // Target counterpart for the flatten-on-update scenario: both parameters exist ungrouped.
            var grpFlatTarget = this.CreateElementDefinition("grpFlat", "GrpFlat", this.targetIteration);
            this.AddParameter(grpFlatTarget, this.massParameterType, "1", "1");
            this.AddParameter(grpFlatTarget, this.powerParameterType, "2", "2");

            // Target counterpart for the group-update scenario: mass is in "Old", power is in "Old"; "Old" is not in source.
            var grpUpdTarget = this.CreateElementDefinition("grpUpd", "GrpUpd", this.targetIteration);
            this.AddParameterGroup(grpUpdTarget, "B", null);
            var targetOld = this.AddParameterGroup(grpUpdTarget, "Old", null);
            this.AddParameter(grpUpdTarget, this.massParameterType, "1", "1").Group = targetOld;
            this.AddParameter(grpUpdTarget, this.powerParameterType, "2", "2").Group = targetOld;

            this.CreateElementDefinition("dupTgt", "Dup Target A", this.targetIteration);
            this.CreateElementDefinition("dupTgt", "Dup Target B", this.targetIteration);

            this.AddToCache(this.targetIteration);
        }

        private void SetupMocks()
        {
            this.sessionService = new Mock<ISessionService>();
            this.sessionService.Setup(x => x.SourceIteration).Returns(this.sourceIteration);
            this.sessionService.Setup(x => x.TargetIteration).Returns(this.targetIteration);
            this.sessionService.Setup(x => x.Cache).Returns(this.assembler.Cache);
            this.sessionService.Setup(x => x.Transactions).Returns(this.transactions);
            this.sessionService.Setup(x => x.DomainOfExpertise).Returns(this.sys);

            this.filterService = new Mock<IFilterService>();

            this.filterService.Setup(x => x.IsMemberOfSelectedCategory(It.IsAny<ElementDefinition>()))
                .Returns((Func<ElementDefinition, bool>)(elementDefinition =>
                    !this.commandArguments.FilteredCategories.Any()
                    || elementDefinition.Category.Any(category => this.commandArguments.FilteredCategories.Contains(category.ShortName))));

            this.filterService.Setup(x => x.IsParameterSpecifiedOrAny(It.IsAny<Parameter>()))
                .Returns((Func<Parameter, bool>)(parameter =>
                    !this.commandArguments.SelectedParameters.Any()
                    || this.commandArguments.SelectedParameters.Contains(parameter.ParameterType.ShortName)));
        }

        private void BuildAndRun(string extraArguments)
        {
            var args = $"-s {BaseUri} -u admin -p pass --action {CommandEnumeration.SyncElementDefinitions} --source-model SRC --target-model TGT --dry {extraArguments}".Split(' ', StringSplitOptions.RemoveEmptyEntries);

            Parser.Default.ParseArguments<Arguments>(args)
                .WithNotParsed(errors => Assert.Fail($"Fail to parse arguments: {string.Join(", ", errors)}"))
                .WithParsed(arguments => this.commandArguments = arguments);

            this.syncCommand = new SyncCommand(this.commandArguments, this.sessionService.Object, this.filterService.Object);
            this.syncCommand.Sync();
        }

        private string ReadReport()
        {
            var reportFile = Path.Combine(Directory.GetCurrentDirectory(), "LastSyncReport.txt");

            Assert.That(File.Exists(reportFile), Is.True, "A sync report file should have been written.");

            return File.ReadAllText(reportFile);
        }

        private ElementDefinition CreateElementDefinition(string shortName, string name, Iteration iteration, params Category[] categories)
        {
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Name = name,
                ShortName = shortName,
                Owner = this.sys,
                Container = iteration
            };

            elementDefinition.Category.AddRange(categories);
            iteration.Element.Add(elementDefinition);

            return elementDefinition;
        }

        private Parameter AddParameter(ElementDefinition elementDefinition, ParameterType parameterType, string published, string reference = "-", ParameterSwitchKind valueSwitch = ParameterSwitchKind.REFERENCE)
        {
            var parameter = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterType = parameterType,
                Owner = this.sys,
                Container = elementDefinition
            };

            var valueSet = new ParameterValueSet(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ValueSwitch = valueSwitch,
                Published = new ValueArray<string>(new[] { published }),
                Reference = new ValueArray<string>(new[] { reference }),
                Manual = new ValueArray<string>(new[] { "-" }),
                Computed = new ValueArray<string>(new[] { "-" }),
                Formula = new ValueArray<string>(new[] { "-" })
            };

            parameter.ValueSet.Add(valueSet);
            elementDefinition.Parameter.Add(parameter);

            return parameter;
        }

        private ParameterGroup AddParameterGroup(ElementDefinition elementDefinition, string name, ParameterGroup containingGroup)
        {
            var parameterGroup = new ParameterGroup(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Name = name,
                ContainingGroup = containingGroup,
                Container = elementDefinition
            };

            elementDefinition.ParameterGroup.Add(parameterGroup);

            return parameterGroup;
        }

        private ElementUsage AddUsage(ElementDefinition primary, ElementDefinition referenced, string shortName, params Category[] categories)
        {
            var usage = new ElementUsage(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Name = shortName,
                ShortName = shortName,
                Owner = this.sys,
                ElementDefinition = referenced,
                Container = primary
            };

            usage.Category.AddRange(categories);
            primary.ContainedElement.Add(usage);

            return usage;
        }

        private void AddOverride(ElementUsage usage, Parameter overriddenParameter, string published)
        {
            var parameterOverride = new ParameterOverride(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                Owner = this.sys,
                Parameter = overriddenParameter,
                Container = usage
            };

            var valueSet = new ParameterOverrideValueSet(Guid.NewGuid(), this.assembler.Cache, this.uri)
            {
                ParameterValueSet = overriddenParameter.ValueSet.First(),
                ValueSwitch = ParameterSwitchKind.REFERENCE,
                Published = new ValueArray<string>(new[] { published }),
                Reference = new ValueArray<string>(new[] { "-" }),
                Manual = new ValueArray<string>(new[] { "-" }),
                Computed = new ValueArray<string>(new[] { "-" }),
                Formula = new ValueArray<string>(new[] { "-" })
            };

            parameterOverride.ValueSet.Add(valueSet);
            usage.ParameterOverride.Add(parameterOverride);
        }

        private void AddToCache(Iteration iteration)
        {
            this.assembler.Cache.TryAdd(new CacheKey(iteration.Iid, null), new Lazy<CDP4Common.CommonData.Thing>(() => iteration));
            this.assembler.Cache.TryAdd(new CacheKey(((EngineeringModel)iteration.Container).Iid, null), new Lazy<CDP4Common.CommonData.Thing>(() => iteration.Container));

            foreach (var elementDefinition in iteration.Element)
            {
                this.assembler.Cache.TryAdd(new CacheKey(elementDefinition.Iid, iteration.Iid), new Lazy<CDP4Common.CommonData.Thing>(() => elementDefinition));

                foreach (var parameterGroup in elementDefinition.ParameterGroup)
                {
                    this.assembler.Cache.TryAdd(new CacheKey(parameterGroup.Iid, iteration.Iid), new Lazy<CDP4Common.CommonData.Thing>(() => parameterGroup));
                }

                foreach (var parameter in elementDefinition.Parameter)
                {
                    this.assembler.Cache.TryAdd(new CacheKey(parameter.Iid, iteration.Iid), new Lazy<CDP4Common.CommonData.Thing>(() => parameter));

                    foreach (var valueSet in parameter.ValueSet)
                    {
                        this.assembler.Cache.TryAdd(new CacheKey(valueSet.Iid, iteration.Iid), new Lazy<CDP4Common.CommonData.Thing>(() => valueSet));
                    }
                }
            }
        }
    }
}
