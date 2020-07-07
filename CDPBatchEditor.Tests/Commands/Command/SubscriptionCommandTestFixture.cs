// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SubscriptionCommandTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Nathanael Smiechowski, Alex Vorobiev, Alexander van Delft, Kamil Wojnowski, Sam Gerené
//
//    This file is part of CDP4 Batch Editor. 
//    The CDP4 Batch Editor is a commandline application to perform batch operations on a 
//    ECSS-E-TM-10-25 Annex A and Annex C data source
//
//    The CDP4 Batch Editor is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or any later version.
//
//    The CDP4 Batch Editor is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU Affero General License for more details.
//
//    You should have received a copy of the GNU Affero General License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
        public void VerifySubscribeToParameters()
        {
            const string parameterUserFriendlyShortName = "testParameter2", elementDefinitionShortName = "testElementDefinition", domain = "testDomain2";
            this.BuildAction($"--action={CommandEnumeration.Subscribe} -m TEST --parameters={parameterUserFriendlyShortName} --element-definition={elementDefinitionShortName} --domain={domain}");

            Assert.IsFalse(this.Parameter2.ParameterSubscription.Any());

            this.subscriptionCommand.Subscribe();

            Assert.AreEqual(1, this.Transactions.First().UpdatedThing.Count);
            Assert.AreEqual(1, this.Transactions.First().AddedThing.Count());

            Assert.IsTrue(this.Transactions.Any(t => t.AddedThing.Any(p => p is ParameterSubscription s && s.Owner == this.Domain2)));

            Assert.IsTrue(this.Transactions.Any(t => t.UpdatedThing.Any(u => u.Value is Parameter p
                                                                             && p.ParameterSubscription.Any(s => s.Owner.ShortName == domain)
                                                                             && p.ParameterType.ShortName == parameterUserFriendlyShortName)));
        }

        [Test]
        public void VerifySetParameterSubscriptionsSwitch()
        {
            const string parameterUserFriendlyShortName = "testParameter3", elementDefinitionShortName = "testElementDefinition";
            this.BuildAction($"--action={CommandEnumeration.Subscribe} --parameter-switch REFERENCE -m TEST --parameters={parameterUserFriendlyShortName} --element-definition={elementDefinitionShortName} --domain=testDomain");

            Assert.IsTrue(this.Parameter4.ParameterSubscription.All(
                s => s.ValueSet.All(
                    v => v.ValueSwitch == ParameterSwitchKind.MANUAL)));

            this.subscriptionCommand.SetParameterSubscriptionsSwitch();

            var things = this.Transactions.SelectMany(t => t.UpdatedThing.Select(a => a.Value)).ToArray();
            Assert.AreEqual(1, things.Count());
            var valueSet = things.First() as ParameterSubscriptionValueSet;
            var parameter = valueSet?.GetContainerOfType<Parameter>();

            Assert.IsNotNull(parameter);
            Assert.IsTrue(parameter.ParameterType.ShortName == parameterUserFriendlyShortName);

            Assert.IsNotNull(valueSet);
            Assert.AreEqual(ParameterSwitchKind.REFERENCE, valueSet.ValueSwitch);
        }
    }
}
