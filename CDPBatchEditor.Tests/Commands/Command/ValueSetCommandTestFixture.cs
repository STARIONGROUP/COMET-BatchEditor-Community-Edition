//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ValueSetCommandTestFixture.cs" company="RHEA System S.A.">
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
            Assert.IsNotNull(elementDefinition);

            var parameter = elementDefinition.Parameter.Where(p => p.ParameterType.ShortName == parameterShortName).ToArray();
            Assert.IsNotEmpty(parameter);

            Assert.IsTrue(
                parameter.All(
                    p => p.ValueSet.Any(
                        v => v.ValueSwitch == ParameterSwitchKind.REFERENCE && v.Reference
                                 .Any(vr => vr == this.ValueSet.Reference.FirstOrDefault()))));

            Assert.IsTrue(
                parameter.All(
                    p => p.ValueSet.Any(
                        v => v.Manual.All(vr => vr == "-"))));

            this.valueSetCommand.MoveReferenceValuesToManualValues();

            var thing = this.Transactions.Select(t => t.AddedThing.Single(a => a is ParameterValueSet)).FirstOrDefault();

            Assert.IsTrue(thing?.GetContainerOfType<Parameter>().ParameterType.ShortName == parameterShortName);

            Assert.IsInstanceOf<ParameterValueSet>(thing);

            var parameterValueSet = (ParameterValueSet) thing;
            Assert.IsTrue(parameterValueSet.ValueSwitch == ParameterSwitchKind.MANUAL);
            Assert.IsTrue(parameterValueSet.Manual.Any(vr => vr == this.ValueSet.Reference.FirstOrDefault()));
        }
    }
}
