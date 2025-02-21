//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="OverrideCommandTestFixture.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2024 Starion Group S.A.
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

    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    [TestFixture]
    public class OverrideCommandTestFixture : BaseCommandTestFixture
    {
        private OverrideCommand overrideCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.overrideCommand = new OverrideCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyOverrideParameters()
        {
            const string parameterUserFriendlyShortName = "testParameter2", elementDefinitionShortName = "testElementDefinition";
            this.BuildAction($"--action={CommandEnumeration.Override} -m TEST --parameters={parameterUserFriendlyShortName} --element-definition={elementDefinitionShortName} --included-owners={this.Domain.ShortName}");

            Assert.That(this.Parameter2.ParameterSubscription.Any(), Is.False);

            this.overrideCommand.Override();

            Assert.That(this.Transactions.First().UpdatedThing.Count, Is.EqualTo(1));
            Assert.That(this.Transactions.First().AddedThing.Count(), Is.EqualTo(1));

            Assert.That(this.Transactions.Any(t => t.AddedThing
                    .Any(p =>
                        p is ParameterOverride s 
                        && s.Owner == this.Domain
                        && s.ParameterType.ShortName == parameterUserFriendlyShortName)),
                Is.True);

            Assert.That(this.Transactions.Any(t => t.UpdatedThing
                    .Any(u => 
                        u.Value is ElementUsage p 
                        && p.ParameterOverride.Any(s => s.Owner.ShortName == this.Domain.ShortName)
                    )),
                Is.True);
        }
    }
}
