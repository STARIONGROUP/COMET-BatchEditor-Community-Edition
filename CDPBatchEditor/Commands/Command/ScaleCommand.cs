//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScaleCommand.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Commands.Command
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Command.Interface;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Defines an <see cref="ScaleCommand" /> that provides actions that are
    /// <see cref="CDP4Common.SiteDirectoryData.MeasurementScale" /> related
    /// </summary>
    public class ScaleCommand : IScaleCommand

    {
        /// <summary>
        /// Gets the injected <see cref="ICommandArguments" /> instance
        /// </summary>
        private readonly ICommandArguments commandArguments;

        /// <summary>
        /// Gets the conversion factor to use to convert values from a <see cref="IValueSet" />
        /// </summary>
        private readonly Dictionary<string, double> conversionFactorsToMilimiters = new Dictionary<string, double>()
        {
            { "km", 1000000 }, { "m", 1000 }, { "dm", 100 }, { "cm", 10 }, { "mm", 1 }, { "μm", 0.001 }, { "nm", 0.000001 }
        };

        /// <summary>
        /// Gets the injected <see cref="IFilterService" /> instance
        /// </summary>
        private readonly IFilterService filterService;

        /// <summary>
        /// Gets the injected <see cref="ISessionService" /> instance
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Initialise a new <see cref="ScaleCommand" />
        /// </summary>
        /// <param name="commandArguments">the <see cref="ICommandArguments" /> arguments instance</param>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        /// <param name="filterService">the <see cref="IFilterService" /></param>
        public ScaleCommand(ICommandArguments commandArguments, ISessionService sessionService, IFilterService filterService)
        {
            this.commandArguments = commandArguments;
            this.sessionService = sessionService;
            this.filterService = filterService;
        }

        /// <summary>
        /// Assign measurement scale to given parameters.
        /// </summary>
        public void AssignMeasurementScale()
        {
            MeasurementScale measurementScale = null;

            if (!string.IsNullOrWhiteSpace(this.commandArguments.Scale))
            {
                measurementScale = this.sessionService.SiteDirectory.SiteReferenceDataLibrary.SelectMany(r => r.Scale).Single(x => x.ShortName == this.commandArguments.Scale);
            }

            if (measurementScale == null)
            {
                Console.WriteLine("Invalid action \"set-scale\": short name of a valid scale must be given in --scale.");
                return;
            }

            if (!this.commandArguments.SelectedParameters.Any())
            {
                Console.WriteLine("No --parameters given. Action set-scale skipped.");
                return;
            }

            foreach (var elementDefinition in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredIn(e)).OrderBy(x => x.ShortName))
            {
                foreach (var parameter in elementDefinition.Parameter.Where(this.filterService.IsParameterSpecifiedOrAny).OrderBy(x => x.ParameterType.ShortName))
                {
                    if (!(parameter.ParameterType is QuantityKind quantityKind))
                    {
                        continue;
                    }

                    var allPossibleScale = quantityKind.AllPossibleScale;

                    if (parameter.Scale == null)
                    {
                        Console.WriteLine(
                            $"No measurement scale assigned to parameter {parameter.UserFriendlyShortName}: should be one of " +
                            $"{string.Join(", ", allPossibleScale.Select(x => x.ShortName).OrderBy(x => x))}");
                    }
                    else if (!allPossibleScale.Contains(parameter.Scale))
                    {
                        Console.WriteLine(
                            $"Invalid measurement scale {parameter.Scale.ShortName} assigned to parameter {parameter.UserFriendlyShortName}: should be one of" +
                            $" {string.Join(", ", allPossibleScale.Select(x => x.ShortName).OrderBy(x => x))}");
                    }

                    if (this.commandArguments.SelectedParameters.Contains(parameter.ParameterType.ShortName)
                        && allPossibleScale.Contains(measurementScale) && parameter.Scale != measurementScale)
                    {
                        var parameterClone = parameter.Clone(true);
                        this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone));
                        parameterClone.Scale = measurementScale;
                        this.sessionService.Transactions.Last().CreateOrUpdate(parameterClone);

                        Console.WriteLine($"Assigned scale \"{measurementScale.ShortName}\" to parameter {parameter.UserFriendlyShortName}");
                    }
                }
            }
        }

        /// <summary>
        /// Standardize dimensions in millimetre for the given iteration.
        /// </summary>
        public void StandardizeDimensionsInMillimetre()
        {
            var mm = this.sessionService.SiteDirectory.SiteReferenceDataLibrary.Select(r => r.Scale.Single(x => x.ShortName == "mm")).Single();

            foreach (var elementDefinition in this.sessionService.Iteration.Element.Where(e => this.filterService.IsFilteredInOrFilterIsEmpty(e)))
            {
                foreach (var dimensionParameterShortName in new[] { "d", "h", "l", "wid" })
                {
                    var parameter = elementDefinition.Parameter
                        .Where(this.filterService.IsParameterSpecifiedOrAny)
                        .SingleOrDefault(x => x.ParameterType.ShortName == dimensionParameterShortName);

                    if (parameter?.ParameterType is QuantityKind quantityKind && quantityKind.AllPossibleScale.Contains(mm)
                                                                              && parameter.Scale.Iid != mm.Iid)
                    {
                        var clone = parameter.Clone(true);
                        var oldScale = clone?.Scale.ShortName;
                        var conversionFactor = this.conversionFactorsToMilimiters[oldScale];
                        this.ConvertParameterValueAndScale(elementDefinition.ShortName, clone, oldScale, mm, conversionFactor);
                    }
                }
            }
        }

        /// <summary>
        /// Convert a parameter value and scale.
        /// </summary>
        /// <param name="elementDefinitionShortName"> The element definition user friendly short name </param>
        /// <param name="parameterClone"> The parameter clone </param>
        /// <param name="oldScale"> The old scale. </param>
        /// <param name="newScale"> The new scale. </param>
        /// <param name="conversionFactor">the conversion factor in <see cref="double" /></param>
        private void ConvertParameterValueAndScale(string elementDefinitionShortName, Parameter parameterClone, string oldScale, MeasurementScale newScale, double conversionFactor)
        {
            var ownerShortName = parameterClone.Owner.ShortName;
            var errorCount = 0;

            foreach (var thingClone in parameterClone.ValueSet.Select(p => p.Clone(true)))
            {
                this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(thingClone), thingClone));

                if (thingClone.Computed.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Computed[0];
                    thingClone.Computed[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, ownerShortName, oldValue, thingClone.Computed[0], errorCount == 0);
                }

                if (thingClone.Manual.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Manual[0];
                    thingClone.Manual[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, ownerShortName, oldValue, thingClone.Manual[0], errorCount == 0);
                }

                if (thingClone.Reference.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Reference[0];
                    thingClone.Reference[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, ownerShortName, oldValue, thingClone.Reference[0], errorCount == 0);
                }

                this.sessionService.Transactions.Last().CreateOrUpdate(thingClone);
            }

            foreach (var thingClone in parameterClone.ParameterSubscription.SelectMany(p => p.ValueSet.Select(v => v.Clone(true))))
            {
                var subscriber = (thingClone.Container as ParameterSubscription)?.Owner.ShortName;
                this.sessionService.Transactions.Add(new ThingTransaction(TransactionContextResolver.ResolveContext(thingClone), thingClone));

                if (thingClone.Computed.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Computed[0];
                    thingClone.Computed[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, subscriber, oldValue, thingClone.Computed[0], errorCount == 0, true);
                }

                if (thingClone.Manual.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Manual[0];
                    thingClone.Manual[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, subscriber, oldValue, thingClone.Manual[0], errorCount == 0, true);
                }

                if (thingClone.Reference.Count == 1 && errorCount == 0)
                {
                    var oldValue = thingClone.Reference[0];
                    thingClone.Reference[0] = this.ConvertNumericValue(oldValue, conversionFactor, ref errorCount);
                    this.OutputReport(elementDefinitionShortName, parameterClone.UserFriendlyShortName, oldScale, newScale, subscriber, oldValue, thingClone.Reference[0], errorCount == 0, true);
                }

                this.sessionService.Transactions.Last().CreateOrUpdate(thingClone);
            }

            if (errorCount == 0)
            {
                var transaction = new ThingTransaction(TransactionContextResolver.ResolveContext(parameterClone), parameterClone);
                parameterClone.Scale = newScale;
                transaction.CreateOrUpdate(parameterClone);
                this.sessionService.Transactions.Add(transaction);
            }
        }

        /// <summary>
        /// Prints informations about the <see cref="IValueSet" /> being updated
        /// </summary>
        /// <param name="elementDefinitionShortName">The element definition.</param>
        /// <param name="parameterShortName">The parameter short name.</param>
        /// <param name="ownerShortName">The owner short name.</param>
        /// <param name="oldScaleShortName">The old scale short name.</param>
        /// <param name="newScale">The new scale</param>
        /// <param name="oldValue">The old value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="hasConversionSucceed">Assertion whether the conversion has succeed</param>
        /// <param name="isSubscription">Assertion whether this is a parameter subscription or not.</param>
        private void OutputReport(string elementDefinitionShortName, string parameterShortName, string oldScaleShortName, IShortNamedThing newScale, string ownerShortName, string oldValue, string newValue, bool hasConversionSucceed = true, bool isSubscription = false)
        {
            var ownership = isSubscription ? "subscribed by" : "owned by";
            var messageStart = $"In {elementDefinitionShortName} parameter \"{parameterShortName}\" ({ownership} {ownerShortName}) value {oldValue} {oldScaleShortName}";
            var message = hasConversionSucceed ? $"{messageStart} is converted to {newValue} {newScale.ShortName}" : $"{messageStart} cannot be converted";
            Console.WriteLine(message);
        }

        /// <summary>
        /// Convert a numeric value from old scale to new scale.
        /// </summary>
        /// <param name="oldValue">
        /// The old value. If the old value is a blank string or null, it is reset to the default value
        /// "-".
        /// </param>
        /// <param name="conversionFactor"> The conversion factor. </param>
        /// <param name="errorCount"> Error count, that is incremented for every conversion error detected. </param>
        /// <returns> The string value converted to the new scale. </returns>
        private string ConvertNumericValue(string oldValue, double conversionFactor, ref int errorCount)
        {
            var newValue = "-";

            if (string.IsNullOrWhiteSpace(oldValue) || oldValue == newValue)
            {
                return newValue;
            }

            if (double.TryParse(oldValue, out var value))
            {
                newValue = (value * conversionFactor).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                errorCount++;
            }

            return newValue;
        }
    }
}
