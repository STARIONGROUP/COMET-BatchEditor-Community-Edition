//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DomainCommandTestFixture.cs" company="Starion Group S.A.">
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
    using CDP4Common.SiteDirectoryData;

    using CDPBatchEditor.Commands.Command;

    using NUnit.Framework;

    [TestFixture]
    public class DomainCommandTestFixture : BaseCommandTestFixture
    {
        private DomainCommand domainCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.domainCommand = new DomainCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        private static void AssertThingHasExpectedOwner(DomainOfExpertise oldOwner, DomainOfExpertise newOwner, IOwnedThing thing)
        {
            Assert.That(thing, Is.Not.Null);
            Assert.That(oldOwner, Is.Not.SameAs(thing.Owner));
            Assert.That(newOwner, Is.SameAs(thing.Owner));


        }

        [Test]
        public void VerifyChangeDomain()
        {
            Assert.That(this.Transactions.Count, Is.EqualTo(0));

            Assert.That(this.Transactions.SelectMany(x => x.UpdatedThing), Is.Empty);

            var action = "--action ChangeDomain -m TEST --parameters testParameter,testParameter2,P_mean  --element-definition testElementDefinition --domain testDomain --to-domain testDomain2";
            this.BuildAction(action);
            this.domainCommand.ChangeDomain();

            Assert.That(this.Transactions.Count, Is.EqualTo(10));

            Assert.That(this.Transactions.SelectMany(x => x.UpdatedThing), Is.Not.Empty);
            Assert.That(this.Transactions.SelectMany(x => x.AddedThing), Is.Empty);

            Assert.That(this.Transactions.All(x => x.UpdatedThing.Any() && x.UpdatedThing.All(y => y.Value is IOwnedThing p && p.Owner == this.Domain2)), Is.True);

            var updatedParameters = this.Transactions.SelectMany(x => x.UpdatedThing.Values.Select(t => t as Parameter).Where(e => e != null));
            var updatedElementDefinitions = this.Transactions.SelectMany(x => x.UpdatedThing.Values.Select(t => t as ElementDefinition).Where(e => e != null));

            foreach (var thing in this.Transactions.SelectMany(x => x.UpdatedThing.Values.Select(t => t as IOwnedThing)))
            {
                AssertThingHasExpectedOwner(this.Domain, this.Domain2, thing);
            }

            Assert.That(updatedElementDefinitions.Single().ShortName == this.TestElementDefinition.ShortName);
        }

        [Test]
        public void VerifyChangeDomainFails()
        {
            Assert.That(this.Transactions.Count, Is.EqualTo(0));

            Assert.That(this.Transactions.SelectMany(x => x.UpdatedThing), Is.Empty);

            var action = "--action ChangeDomain -m TEST --parameters testParameter,testParameter2 --element-definition testElementDefinition --domain bla --to-domain bla2";
            this.BuildAction(action);
            this.domainCommand.ChangeDomain();

            Assert.That(this.Transactions.Count, Is.EqualTo(0));

            action = "--action ChangeDomain -m TEST --parameters testParameter,testParameter2 --element-definition testElementDefinition --domain testDomain --to-domain testDomain";
            this.BuildAction(action);
            this.domainCommand.ChangeDomain();

            Assert.That(this.Transactions.Count, Is.EqualTo(0));

            action = "--action ChangeDomain -m TEST --parameters testParameter,testParameter2 --element-definition testElementDefinition --domain testDomain --to-domain bla2";
            this.BuildAction(action);
            this.domainCommand.ChangeDomain();

            Assert.That(this.Transactions.Count, Is.EqualTo(0));
        }

        [Test]
        public void VerifyChangeParameterOwnership()
        {
            var action = "--action ChangeDomain -m LOFT --parameters testParameter,testParameter2 --element-definition testElementDefinition --domain testDomain2";
            this.BuildAction(action);
            this.domainCommand.ChangeParameterOwnership();

            Assert.That(this.Transactions.Count, Is.EqualTo(2));
            Assert.That(this.Transactions.SelectMany(x => x.UpdatedThing), Is.Not.Empty);

            foreach (var thing in this.Transactions.SelectMany(x => x.UpdatedThing.Values.Select(t => t as IOwnedThing)))
            {
                AssertThingHasExpectedOwner(this.Domain, this.Domain2, thing);
            }
        }

        [Test]
        public void VerifySetGenericEquipmentOwnership()
        {
            var action = "--action SetGenericOwners -m TEST --element-definition testElementDefinition4 --domain testDomain";
            this.BuildAction(action);

            var oldOwner1 = this.Parameter5.Owner;
            var oldOwner2 = this.Parameter6.Owner;
            var oldOwner3 = this.Parameter7.Owner;

            Assert.That(this.Domain, Is.SameAs(oldOwner1));
            Assert.That(this.Domain, Is.SameAs(oldOwner2));
            Assert.That(this.Domain, Is.SameAs(oldOwner3));

            this.domainCommand.SetGenericEquipmentOwnership();

            Assert.That(this.Transactions.Count, Is.EqualTo(3));

            Assert.That(this.Transactions.SelectMany(x => x.UpdatedThing), Is.Not.Empty);
            Assert.That(this.Transactions.SelectMany(x => x.AddedThing), Is.Empty);

            var updateParameters = this.Transactions.SelectMany(x => x.UpdatedThing).Where(u => u.Value is Parameter p).Select(u => u.Value as Parameter).ToList();
            var parameter = updateParameters.FirstOrDefault(x => x.ParameterType.ShortName == this.Parameter5.ParameterType.ShortName);
            AssertThingHasExpectedOwner(oldOwner1, this.Domain3, parameter);
            parameter = updateParameters.FirstOrDefault(x => x.ParameterType.ShortName == this.Parameter6.ParameterType.ShortName);
            AssertThingHasExpectedOwner(oldOwner2, this.Domain4, parameter);
            parameter = updateParameters.FirstOrDefault(x => x.ParameterType.ShortName == this.Parameter7.ParameterType.ShortName);
            AssertThingHasExpectedOwner(oldOwner3, this.Domain5, parameter);
        }
    }
}
