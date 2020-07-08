//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ParameterCommandTestFixture.cs" company="RHEA System S.A.">
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

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    public class ParameterCommandTestFixture : BaseCommandTestFixture
    {
        private ParameterCommand parameterCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.parameterCommand = new ParameterCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyAddParameters()
        {
            const string parameterUserFriendlyShortName = "testParameter2",
                elementDefinitionShortName = "testElementDefinition2",
                parameterGroupdShortName = "DateGroup";

            this.BuildAction($"--action {CommandEnumeration.AddParameters} --parameter-group {parameterGroupdShortName} -m TEST --parameters {parameterUserFriendlyShortName} --element-definition {elementDefinitionShortName} --domain testDomain");

            this.parameterCommand.Add();

            Assert.IsTrue(
                this.SessionService.Object.Transactions.Any(
                    t => t.AddedThing.Any(
                        a => a is Parameter p
                             && p.ParameterType.ShortName == parameterUserFriendlyShortName)));

            Assert.IsTrue(
                string.IsNullOrWhiteSpace(this.CommandArguments.ParameterGroup)
                || this.SessionService.Object.Transactions.Any(
                    t =>
                        t.AddedThing.Any(
                            a => a is ParameterGroup g
                                 && g.Name == parameterGroupdShortName)));

            Assert.IsTrue(
                string.IsNullOrWhiteSpace(this.CommandArguments.ParameterGroup)
                || this.SessionService.Object.Transactions.Any(
                    t =>
                        t.AddedThing.Any(
                            a => a is Parameter p
                                 && p.Group.Name == parameterGroupdShortName
                                 && p.ParameterType.ShortName == parameterUserFriendlyShortName)));
        }

        [Test]
        public void VerifyRemoveParameters()
        {
            const string parameterUserFriendlyShortName = "testParameter2",
                elementDefinitionShortName = "testElementDefinition";

            this.BuildAction($"--action {CommandEnumeration.AddParameters} -m TEST --parameters {parameterUserFriendlyShortName} --element-definition {elementDefinitionShortName} --domain testDomain ");

            this.parameterCommand.Remove();

            Assert.IsTrue(
                this.SessionService.Object.Transactions.Any(
                    t => t.DeletedThing.Any(
                        a => a.ClassKind == ClassKind.Parameter
                             && a.UserFriendlyShortName == $"{elementDefinitionShortName}.{parameterUserFriendlyShortName}")));
        }
    }
}
