//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ReportGeneratorTestFixture.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;

    using CDPBatchEditor.Commands;
    using CDPBatchEditor.Services.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ReportGeneratorTestFixture
    {
        [SetUp]
        public void Setup()
        {
            this.SetupData();
        }

        private const string BaseUri = "http://test.com";

        internal Iteration Iteration { get; private set; }

        public DomainOfExpertise Domain { get; private set; }

        public DomainOfExpertise Domain2 { get; private set; }

        public Parameter Parameter { get; private set; }

        public Parameter Parameter2 { get; private set; }

        public Parameter Parameter3 { get; private set; }

        public Parameter Parameter4 { get; private set; }

        public ActualFiniteStateList ActualPossibleFiniteStateList { get; private set; }

        public SimpleQuantityKind SimpleQuantityKind { get; private set; }

        public RatioScale MillimeterScale { get; private set; }

        public RatioScale MeterScale { get; private set; }

        public RatioScale KilometerScale { get; private set; }

        public ParameterValueSet ValueSet { get; private set; }

        public Assembler Assembler { get; set; }

        public ParameterSubscription ParameterSubscription { get; set; }

        private Uri uri;
        private SiteDirectory siteDirectory;
        private TextParameterType parameterType;
        private TextParameterType parameterType2;
        private SimpleQuantityKind parameterType3;
        private EngineeringModel model;
        private ElementDefinition elementDefinition;
        private EngineeringModelSetup engineeringModelSetup;
        private SiteReferenceDataLibrary siteReferenceDataLibrary;
        private ModelReferenceDataLibrary modelReferenceDataLibrary;
        private Person person;
        private Participant participant;
        private ElementDefinition elementDefinition3;
        private ElementDefinition elementDefinition2;
        private SimpleQuantityKind parameterType4;
        private Mock<ISessionService> sessionService;
        private ReportGenerator reportGenerator;

        private void SetupData()
        {
            this.sessionService = new Mock<ISessionService>();
            this.uri = new Uri(BaseUri);
            this.Assembler = new Assembler(this.uri);
            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), this.Assembler.Cache, this.uri);

            this.SetupDomainPersonAndParticipant();

            this.model = new EngineeringModel(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.Iteration = new Iteration(Guid.NewGuid(), this.Assembler.Cache, this.uri);

            this.siteReferenceDataLibrary = new SiteReferenceDataLibrary(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.siteDirectory.SiteReferenceDataLibrary.Add(this.siteReferenceDataLibrary);

            this.modelReferenceDataLibrary = new ModelReferenceDataLibrary(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                RequiredRdl = this.siteReferenceDataLibrary
            };

            this.SetupScales();
            this.SetupFiniteStates();
            this.SetupElementDefinitionsAndUsages();

            this.model.Iteration.Add(this.Iteration);

            var iterationSetup = new IterationSetup(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.Iteration.IterationSetup = iterationSetup;

            this.engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.Assembler.Cache, this.uri)
                { ShortName = "TEST", Name = "TEST" };

            this.engineeringModelSetup.IterationSetup.Add(iterationSetup);

            this.engineeringModelSetup.RequiredRdl.Add(this.modelReferenceDataLibrary);

            this.model.EngineeringModelSetup = this.engineeringModelSetup;
            this.model.EngineeringModelSetup.Participant.Add(this.participant);

            this.Assembler.Cache.TryAdd(new CacheKey(this.Iteration.Iid, null), new Lazy<Thing>(() => this.Iteration));
            this.Assembler.Cache.TryAdd(new CacheKey(this.model.Iid, null), new Lazy<Thing>(() => this.model));
            this.Assembler.Cache.TryAdd(new CacheKey(this.siteReferenceDataLibrary.Iid, null), new Lazy<Thing>(() => this.siteReferenceDataLibrary));

            this.sessionService.Setup(x => x.Iteration).Returns(this.Iteration);

            this.reportGenerator = new ReportGenerator(this.sessionService.Object);
        }

        private void SetupScales()
        {
            this.MillimeterScale = new RatioScale(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                NumberSet = NumberSetKind.INTEGER_NUMBER_SET,
                MinimumPermissibleValue = "0",
                ShortName = "mm"
            };

            this.MeterScale = new RatioScale(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                NumberSet = NumberSetKind.INTEGER_NUMBER_SET,
                MinimumPermissibleValue = "0",
                ShortName = "m"
            };

            this.KilometerScale = new RatioScale(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                NumberSet = NumberSetKind.INTEGER_NUMBER_SET,
                MinimumPermissibleValue = "0",
                ShortName = "km"
            };

            this.SimpleQuantityKind = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.SimpleQuantityKind.PossibleScale.Add(this.MillimeterScale);
            this.SimpleQuantityKind.PossibleScale.Add(this.MeterScale);
            this.SimpleQuantityKind.PossibleScale.Add(this.KilometerScale);

            this.siteReferenceDataLibrary.Scale.AddRange(this.SimpleQuantityKind.AllPossibleScale);
        }

        private void SetupFiniteStates()
        {
            this.ActualPossibleFiniteStateList = new ActualFiniteStateList(Guid.NewGuid(), this.Assembler.Cache, this.uri);

            var possibleStateList = new PossibleFiniteStateList(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "actualFiniteStateListTest" };
            var possibleState1 = new PossibleFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "1" };
            var possibleState2 = new PossibleFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "2" };
            var possibleState3 = new PossibleFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "3" };
            possibleStateList.PossibleState.Add(possibleState1);
            possibleStateList.PossibleState.Add(possibleState2);
            possibleStateList.PossibleState.Add(possibleState3);

            this.Iteration.PossibleFiniteStateList.Add(possibleStateList);
            this.ActualPossibleFiniteStateList = new ActualFiniteStateList(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            var actualState1 = new ActualFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { PossibleState = new List<PossibleFiniteState> { possibleState1 } };
            var actualState2 = new ActualFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { PossibleState = new List<PossibleFiniteState> { possibleState2 } };
            var actualState3 = new ActualFiniteState(Guid.NewGuid(), this.Assembler.Cache, this.uri) { PossibleState = new List<PossibleFiniteState> { possibleState3 } };
            this.ActualPossibleFiniteStateList.ActualState.Add(actualState1);
            this.ActualPossibleFiniteStateList.ActualState.Add(actualState2);
            this.ActualPossibleFiniteStateList.ActualState.Add(actualState3);

            this.ActualPossibleFiniteStateList.PossibleFiniteStateList.Add(possibleStateList);
            this.Iteration.ActualFiniteStateList.Add(this.ActualPossibleFiniteStateList);
        }

        private void SetupDomainPersonAndParticipant()
        {
            this.Domain = new DomainOfExpertise(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "testDomain", ShortName = "testDomain" };
            this.siteDirectory.Domain.Add(this.Domain);

            this.Domain2 = new DomainOfExpertise(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "testDomain2", ShortName = "testDomain2" };
            this.siteDirectory.Domain.Add(this.Domain2);

            this.person = new Person(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.siteDirectory.Person.Add(this.person);

            this.participant = new Participant(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Person = this.person };
            this.participant.Domain.Add(this.Domain);
        }

        private void SetupElementDefinitionsAndUsages()
        {
            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Name = "testElementDefinition",
                Owner = this.Domain,
                Container = this.Iteration,
                ShortName = "testElementDefinition"
            };

            this.elementDefinition2 = new ElementDefinition(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Name = "testElementDefinition2",
                Owner = this.Domain,
                Container = this.Iteration,
                ShortName = "testElementDefinition2"
            };

            this.elementDefinition3 = new ElementDefinition(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Name = "testElementDefinition3",
                Owner = this.Domain,
                Container = this.Iteration,
                ShortName = "testElementDefinition3"
            };

            this.Iteration.Element.Add(this.elementDefinition);
            this.Iteration.Element.Add(this.elementDefinition2);
            this.Iteration.Element.Add(this.elementDefinition3);

            this.SetupParameter();

            this.elementDefinition.Parameter.Add(this.Parameter);
            this.elementDefinition.Parameter.Add(this.Parameter2);
            this.elementDefinition.Parameter.Add(this.Parameter3);
            this.elementDefinition2.Parameter.Add(this.Parameter4);

            var parameterOverride = new ParameterOverride(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Owner = this.Domain, Parameter = this.Parameter };

            var elementUsage = new ElementUsage(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ElementDefinition = this.elementDefinition };
            elementUsage.ParameterOverride.Add(parameterOverride);
            this.elementDefinition.ContainedElement.Add(elementUsage);
        }

        private void SetupParameter()
        {
            this.parameterType = new TextParameterType(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter" };
            this.parameterType2 = new TextParameterType(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter2" };
            this.parameterType3 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter3" };

            this.parameterType4 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                ShortName = "l",
                PossibleScale = new List<MeasurementScale>()
                {
                    this.MeterScale, this.KilometerScale, this.MillimeterScale
                }
            };

            this.Parameter = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition,
                ParameterType = this.parameterType,
                Owner = this.elementDefinition.Owner
            };

            this.Parameter2 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition,
                ParameterType = this.parameterType2,
                Owner = this.elementDefinition.Owner
            };

            this.ValueSet = new ParameterValueSet
            {
                ValueSwitch = ParameterSwitchKind.REFERENCE,
                Reference = new ValueArray<string>(new List<string>() { "5555" }),
                Manual = new ValueArray<string>(new List<string>() { "-" }),
                Computed = new ValueArray<string>(new List<string>() { "-" })
            };

            this.ParameterSubscription = new ParameterSubscription(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Owner = this.Domain2,
                ValueSet =
                {
                    new ParameterSubscriptionValueSet(Guid.NewGuid(), this.Assembler.Cache, this.uri)
                    {
                        SubscribedValueSet = this.ValueSet
                    }
                }
            };

            this.Parameter3 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition,
                ParameterType = this.parameterType3,
                Owner = this.elementDefinition.Owner,
                IsOptionDependent = true,
                StateDependence = this.ActualPossibleFiniteStateList,
                Scale = this.MeterScale,
                ValueSet = { this.ValueSet },
                ParameterSubscription = { this.ParameterSubscription }
            };

            this.Parameter4 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition2,
                ParameterType = this.parameterType4,
                Owner = this.elementDefinition.Owner,
                Scale = this.MeterScale,
                ValueSet = { this.ValueSet }
            };

            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType2);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType3);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType4);
        }

        [Test]
        public void VerifyParameterToCsv()
        {
            this.reportGenerator.ParametersToCsv();
        }
    }
}
