//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="StateCommandTestFixture.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2024 Starion Group S.A.
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    public class StateCommandTestFixture : BaseCommandTestFixture
    {
        private StateCommand stateCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.stateCommand = new StateCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyApplyStateDependency()
        {
            const string parameterShortName = "testParameter2",
                elementDefinitionShortName = "testElementDefinition";

            this.BuildAction($"--action {CommandEnumeration.ApplyOptionDependence} --state actualFiniteStateListTest -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            Assert.That(this.Iteration.Element
                .FirstOrDefault(e => e.ShortName == elementDefinitionShortName)?
                .Parameter.Where(p => p.ParameterType.ShortName == parameterShortName)
                .All(p => p.StateDependence is null), Is.True);

            this.stateCommand.ApplyOrRemoveStateDependency(false);

            Assert.That(
                this.Transactions.All(
                    t => t.UpdatedThing.All(
                        a => a.Value is Parameter p
                             && p.StateDependence == this.ActualPossibleFiniteStateList
                             && p.ParameterType.ShortName == parameterShortName)), Is.True);
        }

        [Test]
        public void VerifyApplyStateDependencyBadArgs()
        {
            const string parameterShortName = "testParameter2",
                elementDefinitionShortName = "testElementDefinition";

            this.BuildAction($"--action {CommandEnumeration.ApplyOptionDependence} --state actualFiniteStateListTest -m TEST --element-definition {elementDefinitionShortName} --domain testDomain ");

            this.stateCommand.ApplyOrRemoveStateDependency(false);

            Assert.That(this.Transactions, Is.Empty);

            this.BuildAction($"--action {CommandEnumeration.ApplyOptionDependence} --state actualFiniteStateListTestBad -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            this.stateCommand.ApplyOrRemoveStateDependency(false);

            Assert.That(this.Transactions, Is.Empty);
        }

        [Test]
        public void VerifyRemoveStateDependency()
        {
            const string parameterShortName = "testParameter3",
                elementDefinitionShortName = "testElementDefinition";

            this.BuildAction($"--action {CommandEnumeration.RemoveStateDependence} --state actualFiniteStateListTest -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            Assert.That(this.Iteration.Element
                .FirstOrDefault(e => e.ShortName == elementDefinitionShortName)?
                .Parameter.Where(p => p.ParameterType.ShortName == parameterShortName)
                .All(p => p.StateDependence == this.ActualPossibleFiniteStateList), Is.True);

            this.stateCommand.ApplyOrRemoveStateDependency(true);

            Assert.That(
                this.Transactions.All(
                    t => t.UpdatedThing.All(
                        a => a.Value is Parameter p
                             && p.StateDependence is null
                             && p.ParameterType.ShortName == parameterShortName)), Is.True);
        }
    }
}
