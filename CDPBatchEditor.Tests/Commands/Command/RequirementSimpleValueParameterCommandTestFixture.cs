//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SimpleValueParameterCommandTestFixture.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2025 Starion Group S.A.
// 
//     Author: Nathanael Smiechowski, Alex Vorobiev, Alexander van Delft, Sam Gerené
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

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    public class RequirementSimpleValueParameterCommandTestFixture : BaseCommandTestFixture
    {
        private RequirementSimpleParameterValueCommand requirementSimpleParameterValueCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.requirementSimpleParameterValueCommand = new RequirementSimpleParameterValueCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyAddParameters()
        {
            this.BuildAction($"--action {CommandEnumeration.AddRequirementParameters} -m TEST --parameters {this.parameterType2.ShortName} --domain testDomain");

            this.requirementSimpleParameterValueCommand.Add();

            Assert.That(
                this.SessionService.Object.Transactions.Any(
                    t => t.AddedThing.Any(
                        a => a is SimpleParameterValue p
                             && p.ParameterType.ShortName == this.parameterType2.ShortName)), Is.True);
        }

        [Test]
        public void VerifyRemoveParameters()
        {
            this.BuildAction($"--action {CommandEnumeration.RemoveRequirementParameters} -m TEST --parameters {this.parameterType3.ShortName} --domain testDomain ");

            this.requirementSimpleParameterValueCommand.Remove();

            Assert.That(
                this.SessionService.Object.Transactions.Any(
                    t => t.DeletedThing.Any(
                        a => a.ClassKind == ClassKind.SimpleParameterValue
                             && a is SimpleParameterValue p && p.ParameterType.UserFriendlyShortName == $"{this.parameterType3.ShortName}")), Is.True);
        }
    }
}
