//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CommandDispatcherTestFixture.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Tests.Commands
{
    using System;
    using System.Linq;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Commands.Interface;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class CommandDispatcherTestFixture
    {
        [SetUp]
        public void Setup()
        {
            this.commandArguments = new Mock<ICommandArguments>();
            this.parameterCommand = new Mock<IParameterCommand>();
            this.subscriptionCommand = new Mock<ISubscriptionCommand>();
            this.optionCommand = new Mock<IOptionCommand>();
            this.scaleCommand = new Mock<IScaleCommand>();
            this.stateCommand = new Mock<IStateCommand>();
            this.domainCommand = new Mock<IDomainCommand>();
            this.valueSetCommand = new Mock<IValueSetCommand>();
            this.reportGenerator = new Mock<IReportGenerator>();

            this.commandArguments.Setup(x => x.Report).Returns(true);

            this.commandDispatcher = new CommandDispatcher(
                this.commandArguments.Object, this.parameterCommand.Object, this.subscriptionCommand.Object, this.optionCommand.Object,
                this.scaleCommand.Object, this.stateCommand.Object, this.domainCommand.Object, this.valueSetCommand.Object, this.reportGenerator.Object);
        }

        private Mock<ICommandArguments> commandArguments;
        private Mock<IParameterCommand> parameterCommand;
        private Mock<ISubscriptionCommand> subscriptionCommand;
        private Mock<IOptionCommand> optionCommand;
        private Mock<IScaleCommand> scaleCommand;
        private Mock<IStateCommand> stateCommand;
        private Mock<IDomainCommand> domainCommand;
        private Mock<IValueSetCommand> valueSetCommand;
        private Mock<IReportGenerator> reportGenerator;
        private CommandDispatcher commandDispatcher;

        [Test]
        public void VerifyInvoke()
        {
            var commands = Enum.GetValues(typeof(CommandEnumeration)).Cast<CommandEnumeration>().ToArray();
            var callCount = commands.Length;

            foreach (var command in commands)
            {
                this.commandArguments.Setup(x => x.Command).Returns(command);

                try
                {
                    this.commandDispatcher.Invoke();
                }
                catch (NotImplementedException e)
                {
                    callCount--;
                    continue;
                }
            }

            this.domainCommand.Verify(x => x.ChangeDomain(), Times.Once);
            this.domainCommand.Verify(x => x.ChangeParameterOwnership(), Times.Once);
            this.valueSetCommand.Verify(x => x.MoveReferenceValuesToManualValues(), Times.Once);
            this.stateCommand.Verify(x => x.ApplyOrRemoveStateDependency(It.IsAny<bool>()), Times.Exactly(2));
            this.optionCommand.Verify(x => x.ApplyOrRemoveOptionDependency(It.IsAny<bool>()), Times.Exactly(2));
            this.subscriptionCommand.Verify(x => x.Subscribe(), Times.Once);
            this.subscriptionCommand.Verify(x => x.SetParameterSubscriptionsSwitch(), Times.Once);
            this.parameterCommand.Verify(x => x.Add(), Times.Once);
            this.parameterCommand.Verify(x => x.Remove(), Times.Once);
            this.scaleCommand.Verify(x => x.AssignMeasurementScale(), Times.Once);
            this.scaleCommand.Verify(x => x.StandardizeDimensionsInMillimetre(), Times.Once);

            this.reportGenerator.Verify(x => x.ParametersToCsv(), Times.Exactly(callCount));
        }
    }
}
