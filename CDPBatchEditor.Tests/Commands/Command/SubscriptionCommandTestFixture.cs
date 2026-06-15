//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SubscriptionCommandTestFixture.cs" company="Starion Group S.A.">
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

    [TestFixture]
    public class SubscriptionCommandTestFixture : BaseCommandTestFixture
    {
        private SubscriptionCommand subscriptionCommand;

        internal override void BuildAction(string action)
        {
            base.BuildAction(action);
            this.subscriptionCommand = new SubscriptionCommand(this.CommandArguments, this.SessionService.Object, this.FilterService.Object);
        }

        [Test]
        public void VerifySetParameterSubscriptionsSwitch()
        {
            const string parameterUserFriendlyShortName = "testParameter3", elementDefinitionShortName = "testElementDefinition";
            this.BuildAction($"--action={CommandEnumeration.Subscribe} --parameter-switch REFERENCE -m TEST --parameters={parameterUserFriendlyShortName} --element-definition={elementDefinitionShortName} --domain=testDomain");

            Assert.That(this.Parameter4.ParameterSubscription.All(
                s => s.ValueSet.All(
                    v => v.ValueSwitch == ParameterSwitchKind.MANUAL)), Is.True);

            this.subscriptionCommand.SetParameterSubscriptionsSwitch();

            var things = this.Transactions.SelectMany(t => t.UpdatedThing.Select(a => a.Value)).ToArray();
            Assert.That(things.Length, Is.EqualTo(1));
            var valueSet = things.First() as ParameterSubscriptionValueSet;
            var parameter = valueSet?.GetContainerOfType<Parameter>();

            Assert.That(parameter, Is.Not.Null);
            Assert.That(parameter.ParameterType.ShortName == parameterUserFriendlyShortName, Is.True);

            Assert.That(valueSet, Is.Not.Null);
            Assert.That(valueSet.ValueSwitch, Is.EqualTo(ParameterSwitchKind.REFERENCE));
        }

        [Test]
        public void VerifySubscribeToParameters()
        {
            const string parameterUserFriendlyShortName = "testParameter2", elementDefinitionShortName = "testElementDefinition", domain = "testDomain2";
            this.BuildAction($"--action={CommandEnumeration.Subscribe} -m TEST --parameters={parameterUserFriendlyShortName} --element-definition={elementDefinitionShortName} --domain={domain}");

            Assert.That(this.Parameter2.ParameterSubscription.Any(), Is.False);

            this.subscriptionCommand.Subscribe();

            Assert.That(this.Transactions.First().UpdatedThing.Count, Is.EqualTo(1));
            Assert.That(this.Transactions.First().AddedThing.Count(), Is.EqualTo(1));

            Assert.That(this.Transactions.Any(t => t.AddedThing.Any(p => p is ParameterSubscription s && s.Owner == this.Domain2)), Is.True);

            Assert.That(this.Transactions.Any(t => t.UpdatedThing.Any(u => u.Value is Parameter p
                                                                             && p.ParameterSubscription.Any(s => s.Owner.ShortName == domain)
                                                                             && p.ParameterType.ShortName == parameterUserFriendlyShortName)), Is.True);
        }

        [Test]
        public void VerifyBadSubscribeToParameters()
        {
            const string elementDefinitionShortName = "testElementDefinition", domain = "testDomain2";
            this.BuildAction($"--action={CommandEnumeration.Subscribe} -m TEST --element-definition={elementDefinitionShortName} --domain={domain}");

            this.subscriptionCommand.Subscribe();

            Assert.That(this.Transactions, Is.Empty);

            this.BuildAction($"--action={CommandEnumeration.Subscribe} -m TEST --element-definition={elementDefinitionShortName}");

            this.subscriptionCommand.Subscribe();
        }
    }
}
