//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ValueSetCommand.cs" company="Starion Group S.A.">
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

namespace CDPBatchEditor.Commands.Command
{
    using System;
    using System.Linq;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Defines an <see cref="ValueSetCommand" /> that provides actions that are
    /// <see cref="CDP4Common.EngineeringModelData.IValueSet" /> related
    /// </summary>
    public class ValueSetCommand : IValueSetCommand
    {
        /// <summary>
        /// Gets the injected <see cref="ICommandArguments" /> instance
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// Gets the injected <see cref="IFilterService" /> instance
        /// </summary>
        private readonly IFilterService filterService;

        /// <summary>
        /// Gets the injected <see cref="ISessionService" /> instance
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Initialise a new <see cref="ValueSetCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        /// <summary>
        /// Move the value of reference value to manual value on value sets of parameters of specified element definition
        /// </summary>
        public ValueSetCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Move the value of reference value to manual value on value sets of parameters of specified element definition.
        /// The move is only done if the manual value = default value = "-"
        /// </summary>
        public void MoveReferenceValuesToManualValues()
        {
            foreach (var parameter in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e))
                .SelectMany(e => e.Parameter).OrderBy(x => x.ParameterType.ShortName))
            {
                if (parameter.ParameterType is ScalarParameterType && this.filterService.IsParameterSpecifiedOrAny(parameter))
                {
                    var valueSet = parameter.ValueSet.FirstOrDefault();
                    var refValue = valueSet?.Reference;
                    var manualValue = valueSet?.Manual;

                    if ((parameter.ParameterType is ScalarParameterType) || (parameter.ParameterType is EnumerationParameterType))
                        if (valueSet ?.ValueSwitch == ParameterSwitchKind.REFERENCE && manualValue[0] == "-" && refValue[0] != "-")
                        {
                        var valueSetClone = valueSet.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                        valueSetClone.Manual[0] = refValue[0];
                        valueSetClone.ValueSwitch = ParameterSwitchKind.MANUAL;
                        valueSetClone.Reference[0] = "-";
                        transaction.CreateOrUpdate(valueSetClone);
                        this.sessionService.Transactions.Add(transaction);

                        Console.WriteLine($"Moved {parameter.UserFriendlyShortName} = {refValue[0]} ref value to manual value and changed switch to MANUAL");
                        }
                    else if (parameter.ParameterType is CompoundParameterType compType)
                        {
                            var valueSetClone = valueSet.Clone(true);
                            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                            foreach (ParameterTypeComponent comp in compType.Component)
                            {
                                if (manualValue[comp.Index] == "-" && refValue[comp.Index] != "-")
                                {
                                    valueSetClone.Manual[comp.Index] = refValue[comp.Index];
                                    valueSetClone.ValueSwitch = ParameterSwitchKind.MANUAL;
                                    valueSetClone.Reference[comp.Index] = "-";
                                    transaction.CreateOrUpdate(valueSetClone);
                                    this.sessionService.Transactions.Add(transaction);
                                    Console.WriteLine($"Moved {parameter.UserFriendlyShortName}.{comp.ShortName} = {refValue[comp.Index]} reference value to manual value and changed switch to MANUAL");
                                }

                            }

                        }
                }
            }
        }
            

        


        public void MoveManualValuesToReferenceValues()
        {
            foreach (var parameter in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e))
                .SelectMany(e => e.Parameter).OrderBy(x => x.ParameterType.ShortName))
            {
                if (this.filterService.IsParameterSpecifiedOrAny(parameter))
                {
                    var valueSet = parameter.ValueSet.FirstOrDefault();
                    var refValue = valueSet?.Reference;
                    var manualValue = valueSet?.Manual;

                    if ((parameter.ParameterType is ScalarParameterType) || (parameter.ParameterType is EnumerationParameterType)) { 
                        if (valueSet?.ValueSwitch == ParameterSwitchKind.MANUAL && refValue[0] == "-" && manualValue[0] != "-")
                        {
                            var valueSetClone = valueSet.Clone(true);
                            var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                            valueSetClone.Reference[0] = manualValue[0];
                            valueSetClone.ValueSwitch = ParameterSwitchKind.REFERENCE;
                            valueSetClone.Manual[0] = "-";
                            transaction.CreateOrUpdate(valueSetClone);
                            this.sessionService.Transactions.Add(transaction);

                            Console.WriteLine($"Moved {parameter.UserFriendlyShortName} = {manualValue[0]} manual value to reference value and changed switch to REFERENCE");
                        }
                }
                else if (parameter.ParameterType is CompoundParameterType compType)
                    if (valueSet?.ValueSwitch == ParameterSwitchKind.MANUAL)
                    {
                        var valueSetClone = valueSet.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                        foreach (ParameterTypeComponent comp in compType.Component)
                        {
                            if (refValue[comp.Index] == "-" && manualValue[comp.Index] != "-")
                                {
                                    valueSetClone.Reference[comp.Index] = manualValue[comp.Index];
                                    valueSetClone.ValueSwitch = ParameterSwitchKind.REFERENCE;
                                    valueSetClone.Manual[comp.Index] = "-";
                                    transaction.CreateOrUpdate(valueSetClone);
                                    this.sessionService.Transactions.Add(transaction);
                                    Console.WriteLine($"Moved {parameter.UserFriendlyShortName}.{comp.ShortName} = {manualValue[comp.Index]} manual value to reference value and changed switch to REFERENCE");
                                }

                        }

                    }
                }
            }
        }

        public void SetValueSwitchToReference()
        {
            foreach (var parameter in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e))
                .SelectMany(e => e.Parameter).OrderBy(x => x.ParameterType.ShortName))
            {
                if (this.filterService.IsParameterSpecifiedOrAny(parameter))
                {
                    var valueSet = parameter.ValueSet.FirstOrDefault();


                    if (valueSet?.ValueSwitch != ParameterSwitchKind.REFERENCE)
                    {
                        var valueSetClone = valueSet.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                        valueSetClone.ValueSwitch = ParameterSwitchKind.REFERENCE;
                        transaction.CreateOrUpdate(valueSetClone);
                        this.sessionService.Transactions.Add(transaction);

                        Console.WriteLine($"Changed {parameter.UserFriendlyShortName}  switch to REFERENCE");
                    }
                }
            }
        }

        public void SetValueSwitchToComputed()
        {
            foreach (var parameter in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e))
                .SelectMany(e => e.Parameter).OrderBy(x => x.ParameterType.ShortName))
            {
                if (this.filterService.IsParameterSpecifiedOrAny(parameter))
                {
                    var valueSet = parameter.ValueSet.FirstOrDefault();


                    if (valueSet?.ValueSwitch != ParameterSwitchKind.COMPUTED)
                    {
                        var valueSetClone = valueSet.Clone(true);
                        var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(valueSetClone), valueSetClone);
                        valueSetClone.ValueSwitch = ParameterSwitchKind.COMPUTED;
                        transaction.CreateOrUpdate(valueSetClone);
                        this.sessionService.Transactions.Add(transaction);

                        Console.WriteLine($"Changed {parameter.UserFriendlyShortName}  switch to COMPUTED");
                    }
                }
            }
        }
    }
}
