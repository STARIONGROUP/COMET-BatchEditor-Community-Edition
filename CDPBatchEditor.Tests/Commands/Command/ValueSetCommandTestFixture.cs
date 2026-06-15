//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ValueSetCommandTestFixture.cs" company="Starion Group S.A.">
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

    public class ValueSetCommandTestFixture : BaseCommandTestFixture
    {
        private ValueSetCommand valueSetCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.valueSetCommand = new ValueSetCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyMoveReferenceValuesToManualValues()
        {
            const string parameterShortName = "l",
                elementDefinitionShortName = "testElementDefinition2";

            this.BuildAction($"--action {CommandEnumeration.MoveReferenceValuesToManualValues} -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            var elementDefinition = this.Iteration.Element.FirstOrDefault(e => e.ShortName == elementDefinitionShortName);
            Assert.That(elementDefinition, Is.Not.Null);

            var parameter = elementDefinition.Parameter.Where(p => p.ParameterType.ShortName == parameterShortName).ToArray();
            Assert.That(parameter, Is.Not.Empty);

            Assert.That(
                parameter.All(
                    p => p.ValueSet.Any(
                        v => v.ValueSwitch == ParameterSwitchKind.REFERENCE && v.Reference
                                 .Any(vr => vr == this.ValueSet.Reference.FirstOrDefault()))), Is.True);

            Assert.That(
                parameter.All(
                    p => p.ValueSet.Any(
                        v => v.Manual.All(vr => vr == "-"))), Is.True);

            this.valueSetCommand.MoveReferenceValuesToManualValues();

            var thing = this.Transactions.Select(t => t.AddedThing.Single(a => a is ParameterValueSet)).FirstOrDefault();

            Assert.That(thing?.GetContainerOfType<Parameter>().ParameterType.ShortName == parameterShortName, Is.True);

            Assert.That(thing, Is.InstanceOf<ParameterValueSet>());

            var parameterValueSet = (ParameterValueSet) thing;
            Assert.That(parameterValueSet.ValueSwitch == ParameterSwitchKind.MANUAL, Is.True);
            Assert.That(parameterValueSet.Manual.Any(vr => vr == this.ValueSet.Reference.FirstOrDefault()), Is.True);
        }
    }
}
