//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ReportGenerator.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDPBatchEditor.Commands.Interface;
    using CDPBatchEditor.Services;
    using CDPBatchEditor.Services.Interfaces;

    /// <summary>
    /// Write cvs reports on specified <see cref="EngineeringModel" />
    /// </summary>
    public class ReportGenerator : IReportGenerator
    {
        /// <summary>
        /// Gets the hard coded column name of the generated report
        /// </summary>
        private readonly IList<string> headers = new List<string>()
        {
            nameof(EngineeringModel), nameof(ElementDefinition), $"{nameof(ElementDefinition)}.{nameof(ElementDefinition.ShortName)}", nameof(DomainOfExpertise),
            $"{nameof(ParameterSubscription)}.{nameof(ParameterSubscription.Owner)}", nameof(Category), nameof(ParameterGroup), nameof(ReferenceDataLibrary),
            nameof(Parameter), nameof(Parameter.UserFriendlyShortName), nameof(ActualFiniteState.ShortName), nameof(Option.ShortName),
            nameof(ParameterValueSet.ActualValue), nameof(ParameterValueSet.Published), nameof(MeasurementScale), nameof(ParameterSwitchKind),
            nameof(ParameterSwitchKind.COMPUTED), nameof(ParameterSwitchKind.MANUAL), nameof(ParameterSwitchKind.REFERENCE), nameof(ParameterValueSet.Formula)
        };

        /// <summary>
        /// Gets the injected <see cref="ISessionService" /> instance
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Initialise a new <see cref="ReportGenerator" />
        /// </summary>
        /// <param name="sessionService">
        /// the <see cref="ISessionService" /> providing the <see cref="CDP4Dal.ISession" /> for the
        /// application
        /// </param>
        public ReportGenerator(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }

        /// <summary>
        /// Report all parameters on the given <see cref="Iteration" />.
        /// </summary>
        public void ParametersToCsv()
        {
            var engineeringModelShortName = (this.sessionService.Iteration.Container as EngineeringModel)?.EngineeringModelSetup.ShortName;
            var csvPath = engineeringModelShortName + "_parameters_report.csv";

            try
            {
                Console.WriteLine($"Report on parameter ({csvPath}) building in progress");
                using var csvFileWriter = new CsvFileWriter(csvPath);

                csvFileWriter.WriteRow(this.headers);

                foreach (var elementDefinition in this.sessionService.Iteration.Element.OrderBy(x => x.ShortName))
                {
                    this.WriteElementDefinition(csvFileWriter, engineeringModelShortName, elementDefinition);

                    foreach (var parameter in elementDefinition.Parameter.OrderBy(x => x.ParameterType.ShortName))
                    {
                        foreach (var parameterValueSet in parameter.ValueSet)
                        {
                            this.WriteParameterRow(csvFileWriter, engineeringModelShortName, parameterValueSet);

                            foreach (var parameterSubscription in parameter.ParameterSubscription.OrderBy(x => x.Owner.ShortName))
                            {
                                foreach (var parameterSubscriptionValueSet in parameterSubscription.ValueSet)
                                {
                                    this.WriteParameterRow(csvFileWriter, engineeringModelShortName, parameterSubscriptionValueSet);
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Done building the report located at: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Write element definition.
        /// </summary>
        /// <param name="csvFileWriter"> The CSV file writer. </param>
        /// <param name="engineeringModelShortName"> The engineering model short name. </param>
        /// <param name="elementDefinition"> The element definition. </param>
        private void WriteElementDefinition(CsvFileWriter csvFileWriter, string engineeringModelShortName, ElementBase elementDefinition)
        {
            var fields = new string[this.headers.Count];
            fields[this.headers.IndexOf(nameof(EngineeringModel))] = engineeringModelShortName;
            fields[this.headers.IndexOf(nameof(ElementDefinition))] = elementDefinition.Name;
            fields[this.headers.IndexOf($"{nameof(ElementDefinition)}.{nameof(ElementDefinition.ShortName)}")] = elementDefinition.ShortName;
            fields[this.headers.IndexOf(nameof(DomainOfExpertise))] = elementDefinition.Owner.ShortName;
            fields[this.headers.IndexOf($"{nameof(ParameterSubscription)}.{nameof(ParameterSubscription.Owner)}")] = "";
            fields[this.headers.IndexOf(nameof(Category))] = string.Join(", ", elementDefinition.Category.Select(cat => cat.ShortName).OrderBy(sn => sn));
            fields[this.headers.IndexOf(nameof(ParameterGroup))] = "";
            fields[this.headers.IndexOf(nameof(ReferenceDataLibrary))] = "";
            fields[this.headers.IndexOf(nameof(Parameter))] = "";
            fields[this.headers.IndexOf(nameof(Parameter.UserFriendlyShortName))] = "";
            fields[this.headers.IndexOf(nameof(ActualFiniteState.ShortName))] = "";
            fields[this.headers.IndexOf(nameof(Option.ShortName))] = "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.ActualValue))] = "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Published))] = "";
            fields[this.headers.IndexOf(nameof(MeasurementScale))] = "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind))] = "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.COMPUTED))] = "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.MANUAL))] = "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.REFERENCE))] = "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Formula))] = "";
            csvFileWriter.WriteRow(fields);
        }

        /// <summary>
        /// Write parameter row.
        /// </summary>
        /// <param name="csvFileWriter">
        /// The CSV file writer.
        /// </param>
        /// <param name="this.headers">
        /// The this.headers.
        /// </param>
        /// <param name="engineeringModelShortName">
        /// The engineering model short name.
        /// </param>
        /// <param name="parameterValueSet">
        /// The parameter value set.
        /// </param>
        private void WriteParameterRow(CsvFileWriter csvFileWriter, string engineeringModelShortName, ParameterValueSet parameterValueSet)
        {
            var elementDefinition = parameterValueSet.GetContainerOfType<ElementDefinition>();
            var containerParameter = parameterValueSet.GetContainerOfType<Parameter>();
            var fields = new string[this.headers.Count];

            fields[this.headers.IndexOf(nameof(EngineeringModel))] = engineeringModelShortName;
            fields[this.headers.IndexOf(nameof(ElementDefinition))] = elementDefinition.Name;
            fields[this.headers.IndexOf($"{nameof(ElementDefinition)}.{nameof(ElementDefinition.ShortName)}")] = elementDefinition.ShortName;
            fields[this.headers.IndexOf(nameof(DomainOfExpertise))] = parameterValueSet.Owner.ShortName;
            fields[this.headers.IndexOf($"{nameof(ParameterSubscription)}.{nameof(ParameterSubscription.Owner)}")] = "";
            fields[this.headers.IndexOf(nameof(Category))] = string.Join(", ", elementDefinition.Category.Select(cat => cat.ShortName).OrderBy(sn => sn));
            fields[this.headers.IndexOf(nameof(ParameterGroup))] = parameterValueSet.GetContainerOfType<ParameterGroup>()?.UserFriendlyShortName;
            fields[this.headers.IndexOf(nameof(ReferenceDataLibrary))] = string.Join(", ", containerParameter.ParameterType.RequiredRdls.Select(x => x.ShortName));
            fields[this.headers.IndexOf(nameof(Parameter))] = containerParameter.ParameterType.UserFriendlyShortName;
            fields[this.headers.IndexOf(nameof(Parameter.UserFriendlyShortName))] = containerParameter.ParameterType.ShortName;
            fields[this.headers.IndexOf(nameof(ActualFiniteState.ShortName))] = parameterValueSet.ActualState?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(Option.ShortName))] = parameterValueSet.ActualOption?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.ActualValue))] = parameterValueSet.ActualValue?.FirstOrDefault() ?? "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Published))] = parameterValueSet.Published?.FirstOrDefault() ?? "";
            fields[this.headers.IndexOf(nameof(MeasurementScale))] = containerParameter.Scale?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind))] = parameterValueSet.ValueSwitch.ToString();
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.COMPUTED))] = string.Join("|", parameterValueSet.Computed.Select(x => x ?? ""));
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.MANUAL))] = string.Join("|", parameterValueSet.Manual.Select(x => x ?? ""));
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.REFERENCE))] = string.Join("|", parameterValueSet.Reference.Select(x => x ?? ""));
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Formula))] = string.Join("|", parameterValueSet.Formula.Select(s => $"\"{s}\""));
            csvFileWriter.WriteRow(fields);
        }

        /// <summary>
        /// Write parameter row.
        /// </summary>
        /// <param name="csvFileWriter"> The CSV file writer. </param>
        /// <param name="engineeringModelShortName"> The engineering model short name. </param>
        /// <param name="subscriptionValueSet"> The subscription value set. </param>
        private void WriteParameterRow(CsvFileWriter csvFileWriter, string engineeringModelShortName, ParameterSubscriptionValueSet subscriptionValueSet)
        {
            var fields = new string[this.headers.Count];
            var elementDefinition = subscriptionValueSet.GetContainerOfType<ElementDefinition>();
            var parameterOrOverrideBase = subscriptionValueSet.GetContainerOfType<ParameterOrOverrideBase>();
            fields[this.headers.IndexOf(nameof(EngineeringModel))] = engineeringModelShortName;
            fields[this.headers.IndexOf(nameof(ElementDefinition))] = elementDefinition.Name;
            fields[this.headers.IndexOf($"{nameof(ElementDefinition)}.{nameof(ElementDefinition.ShortName)}")] = elementDefinition.ShortName;
            fields[this.headers.IndexOf(nameof(DomainOfExpertise))] = parameterOrOverrideBase.Owner.ShortName;
            fields[this.headers.IndexOf($"{nameof(ParameterSubscription)}.{nameof(ParameterSubscription.Owner)}")] = subscriptionValueSet.Owner.ShortName;
            fields[this.headers.IndexOf(nameof(Category))] = string.Join(", ", elementDefinition.Category.Select(cat => cat.ShortName).OrderBy(sn => sn));
            fields[this.headers.IndexOf(nameof(ParameterGroup))] = subscriptionValueSet.SubscribedValueSet.GetContainerOfType<ParameterGroup>()?.UserFriendlyShortName;
            fields[this.headers.IndexOf(nameof(ReferenceDataLibrary))] = string.Join(", ", parameterOrOverrideBase.ParameterType.RequiredRdls.Select(x => x.ShortName));
            fields[this.headers.IndexOf(nameof(Parameter))] = parameterOrOverrideBase.ParameterType.Name;
            fields[this.headers.IndexOf(nameof(Parameter.UserFriendlyShortName))] = parameterOrOverrideBase.ParameterType.ShortName;
            fields[this.headers.IndexOf(nameof(ActualFiniteState.ShortName))] = subscriptionValueSet.ActualState?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(Option.ShortName))] = subscriptionValueSet.ActualOption?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(ParameterValueSet.ActualValue))] = subscriptionValueSet.ActualValue.FirstOrDefault();
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Published))] = "";
            fields[this.headers.IndexOf(nameof(MeasurementScale))] = parameterOrOverrideBase.Scale?.ShortName ?? "";
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind))] = subscriptionValueSet.ValueSwitch.ToString();
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.COMPUTED))] = string.Join("|", subscriptionValueSet.Computed);
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.MANUAL))] = string.Join("|", subscriptionValueSet.Manual);
            fields[this.headers.IndexOf(nameof(ParameterSwitchKind.REFERENCE))] = string.Join("|", subscriptionValueSet.Reference);
            fields[this.headers.IndexOf(nameof(ParameterValueSet.Formula))] = "";
            csvFileWriter.WriteRow(fields);
        }
    }
}
