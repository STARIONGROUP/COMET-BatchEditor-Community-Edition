//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScaleCommandTestFixture.cs" company="RHEA System S.A.">
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

    public class ScaleCommandTestFixture : BaseCommandTestFixture
    {
        private ScaleCommand scaleCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.scaleCommand = new ScaleCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyAssignMeasurementScale()
        {
            const string parameterShortName = "l",
                elementDefinitionShortName = "testElementDefinition2";

            this.BuildAction($"--action {CommandEnumeration.SetScale} --scale {this.KilometerScale.ShortName} -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            Assert.IsTrue(this.Iteration.Element
                .FirstOrDefault(e => e.ShortName == elementDefinitionShortName)?
                .Parameter.Where(p => p.ParameterType.ShortName == parameterShortName)
                .All(p => p.Scale != this.KilometerScale));

            this.scaleCommand.AssignMeasurementScale();

            Assert.IsTrue(
                this.Transactions.All(
                    t => t.UpdatedThing.All(
                        a => a.Value is Parameter p
                             && p.Scale == this.KilometerScale
                             && p.ParameterType.ShortName == parameterShortName)));
        }

        [Test]
        public void VerifyStandardizeDimensionsInMillimetre()
        {
            const string parameterShortName = "l",
                elementDefinitionShortName = "testElementDefinition2";

            this.BuildAction($"--action {CommandEnumeration.StandardizeDimensionsInMillimeter} -m TEST --parameters {parameterShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            Assert.IsTrue(this.Iteration.Element
                .FirstOrDefault(e => e.ShortName == elementDefinitionShortName)?
                .Parameter.Where(p => p.ParameterType.ShortName == parameterShortName)
                .All(p => p.Scale != this.MillimeterScale));

            this.scaleCommand.StandardizeDimensionsInMillimetre();

            Assert.IsTrue(
                this.Transactions.Any(
                    t => t.UpdatedThing.Any(
                             a => a.Value is Parameter p
                                  && p.Scale == this.MillimeterScale
                                  && p.ParameterType.ShortName == parameterShortName) || t.UpdatedThing.Any(
                             a => a.Value is ParameterSubscription p
                                  && p.Scale == this.MillimeterScale
                                  && p.ParameterType.ShortName == parameterShortName)));
        }
    }
}
