//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CommandDispatcher.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Commands
{
    using System;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Commands.Interface;

    /// <summary>
    /// The <see cref="CommandDispatcher" /> is responsible for invoking the right business class
    /// </summary>
    public class CommandDispatcher : ICommandDispatcher
    {
        /// <summary>
        /// the <see cref="ICommandArguments" /> arguments instance
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// The <see cref="IDomainCommand" /> command instance
        /// </summary>
        private readonly IDomainCommand domainCommand;

        /// <summary>
        /// The <see cref="IOptionCommand" /> command instance
        /// </summary>
        private readonly IOptionCommand optionCommand;

        /// <summary>
        /// The <see cref="IParameterCommand" /> command instance
        /// </summary>
        private readonly IParameterCommand parameterCommand;

        /// <summary>
        /// The <see cref="IReportGenerator" /> instance
        /// </summary>
        private readonly IReportGenerator reportGenerator;

        /// <summary>
        /// The <see cref="IScaleCommand" /> command instance
        /// </summary>
        private readonly IScaleCommand scaleCommand;

        /// <summary>
        /// The <see cref="IOptionCommand" /> command instance
        /// </summary>
        private readonly IStateCommand stateCommand;

        /// <summary>
        /// The <see cref="ISubscriptionCommand" /> command instance
        /// </summary>
        private readonly ISubscriptionCommand subscriptionCommand;

        /// <summary>
        /// The <see cref="IValueSetCommand" /> command instance
        /// </summary>
        private readonly IValueSetCommand valueSetCommand;

        /// <summary>
        /// Initialize a new CommandDispatcher
        /// </summary>
        /// <param name="commandArguments">the command line options</param>
        /// <param name="parameterCommand">the parameter command</param>
        /// <param name="subscriptionCommand">the subscription command</param>
        /// <param name="optionCommand">the option command</param>
        /// <param name="scaleCommand">the scale command</param>
        /// <param name="stateCommand">the state command</param>
        /// <param name="domainCommand">the domain command</param>
        /// <param name="valueSetCommand">the value set command</param>
        /// <param name="reportGenerator">the reportgernerator command</param>
        public CommandDispatcher(
            ICommandArguments commandArguments,
            IParameterCommand parameterCommand,
            ISubscriptionCommand subscriptionCommand,
            IOptionCommand optionCommand,
            IScaleCommand scaleCommand,
            IStateCommand stateCommand,
            IDomainCommand domainCommand,
            IValueSetCommand valueSetCommand,
            IReportGenerator reportGenerator)
        {
            this.commandArguments = commandArguments;
            this.parameterCommand = parameterCommand;
            this.subscriptionCommand = subscriptionCommand;
            this.optionCommand = optionCommand;
            this.scaleCommand = scaleCommand;
            this.stateCommand = stateCommand;
            this.domainCommand = domainCommand;
            this.valueSetCommand = valueSetCommand;
            this.reportGenerator = reportGenerator;
        }

        /// <summary>
        /// Invokes the correct command.
        /// </summary>
        public void Invoke()
        {
            switch (this.commandArguments.Command)
            {
                case CommandEnumeration.AddParameters:
                    this.parameterCommand.Add();
                    break;
                case CommandEnumeration.RemoveParameters:
                    this.parameterCommand.Remove();
                    break;
                case CommandEnumeration.MoveReferenceValuesToManualValues:
                    this.valueSetCommand.MoveReferenceValuesToManualValues();
                    break;
                case CommandEnumeration.ApplyOptionDependence:
                    this.optionCommand.ApplyOrRemoveOptionDependency(false);
                    break;
                case CommandEnumeration.ApplyStateDependence:
                    this.stateCommand.ApplyOrRemoveStateDependency(false);
                    break;
                case CommandEnumeration.ChangeParameterOwnership:
                    this.domainCommand.ChangeParameterOwnership();
                    break;
                case CommandEnumeration.ChangeDomain:
                    this.domainCommand.ChangeDomain();
                    break;
                case CommandEnumeration.RemoveOptionDependence:
                    this.optionCommand.ApplyOrRemoveOptionDependency(true);
                    break;
                case CommandEnumeration.RemoveStateDependence:
                    this.stateCommand.ApplyOrRemoveStateDependency(true);
                    break;
                case CommandEnumeration.SetGenericOwners:
                    this.domainCommand.SetGenericEquipmentOwnership();
                    break;
                case CommandEnumeration.SetScale:
                    this.scaleCommand.AssignMeasurementScale();
                    break;
                case CommandEnumeration.StandardizeDimensionsInMillimeter:
                    this.scaleCommand.StandardizeDimensionsInMillimetre();
                    break;
                case CommandEnumeration.SetSubscriptionSwitch:
                    this.subscriptionCommand.SetParameterSubscriptionsSwitch();
                    break;
                case CommandEnumeration.Subscribe:
                    this.subscriptionCommand.Subscribe();
                    break;
                case CommandEnumeration.Unspecified:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (this.commandArguments.Report)
            {
                this.reportGenerator.ParametersToCsv();
            }
        }
    }
}
