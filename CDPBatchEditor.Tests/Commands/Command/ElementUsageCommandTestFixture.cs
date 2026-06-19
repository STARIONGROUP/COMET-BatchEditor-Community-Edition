//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ElementUsageCommandTestFixture.cs" company="Starion Group S.A.">
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
    using System.Linq;

    using CDP4Common.EngineeringModelData;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    [TestFixture]
    public class ElementUsageCommandTestFixture : BaseCommandTestFixture
    {
        private ElementUsageCommand elementUsageCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.elementUsageCommand = new ElementUsageCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifyElementUsageNamesAreSynchronised()
        {
            this.BuildAction($"--action={CommandEnumeration.SyncElementUsageNames} -m TEST --element-definition={this.TestElementDefinition.ShortName}");

            var elementUsage = this.TestElementDefinition.ContainedElement.Single();

            Assert.That(elementUsage.ShortName, Is.Not.EqualTo(this.TestElementDefinition.ShortName));

            this.elementUsageCommand.SyncNames();

            Assert.That(this.Transactions.Count, Is.EqualTo(1));

            var updatedUsage = this.Transactions.Single().UpdatedThing.Values.OfType<ElementUsage>().Single();

            Assert.Multiple(() =>
            {
                Assert.That(updatedUsage.ShortName, Is.EqualTo(this.TestElementDefinition.ShortName));
                Assert.That(updatedUsage.Name, Is.EqualTo(this.TestElementDefinition.Name));
            });
        }

        [Test]
        public void VerifyAlreadyMatchingElementUsageIsNotRewritten()
        {
            var elementUsage = this.TestElementDefinition.ContainedElement.Single();
            elementUsage.ShortName = this.TestElementDefinition.ShortName;
            elementUsage.Name = this.TestElementDefinition.Name;

            this.BuildAction($"--action={CommandEnumeration.SyncElementUsageNames} -m TEST --element-definition={this.TestElementDefinition.ShortName}");

            this.elementUsageCommand.SyncNames();

            Assert.That(this.Transactions, Is.Empty);
        }

        [Test]
        public void VerifyUsagesOutsideTheFilterAreNotSynchronised()
        {
            // Only element definition "testElementDefinition2" is in scope; the single usage references "testElementDefinition".
            this.BuildAction($"--action={CommandEnumeration.SyncElementUsageNames} -m TEST --element-definition=testElementDefinition2");

            this.elementUsageCommand.SyncNames();

            Assert.That(this.Transactions, Is.Empty);
        }
    }
}