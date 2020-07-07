// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseCommandTestFixture.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Services.Interfaces;

    using CommandLine;

    using Moq;

    using NUnit.Framework;

    public abstract class BaseCommandTestFixture
    {
        private const string BaseUri = "http://test.com";
        private readonly string baseArguments = $" -s {BaseUri} -u admin -p pass ";

        internal ICommandArguments CommandArguments { get; private set; }

        internal Mock<ISessionService> SessionService { get; private set; }

        internal Mock<IFilterService> FilterService { get; private set; }

        internal List<ThingTransaction> Transactions { get; private set; } = new List<ThingTransaction>();

        internal Iteration Iteration { get; private set; }

        internal Mock<ISession> Session { get; private set; }

        public DomainOfExpertise Domain { get; private set; }

        public DomainOfExpertise Domain2 { get; private set; }

        public DomainOfExpertise Domain3 { get; private set; }

        public DomainOfExpertise Domain4 { get; private set; }

        public DomainOfExpertise Domain5 { get; private set; }

        public Parameter Parameter { get; private set; }

        public Parameter Parameter2 { get; private set; }

        public Parameter Parameter3 { get; private set; }
        public Parameter Parameter3s { get; private set; }

        public Parameter Parameter4 { get; private set; }

        public Parameter Parameter6 { get; private set; }

        public Parameter Parameter7 { get; private set; }

        public Parameter Parameter5 { get; private set; }

        public ActualFiniteStateList ActualPossibleFiniteStateList { get; private set; }

        public SimpleQuantityKind SimpleQuantityKind { get; private set; }

        public RatioScale MillimeterScale { get; private set; }

        public RatioScale MeterScale { get; private set; }

        public RatioScale KilometerScale { get; private set; }

        public ParameterValueSet ValueSet { get; private set; }

        public Assembler Assembler { get; set; }

        public ParameterSubscription ParameterSubscription { get; set; }
        public ParameterSubscription ParameterSubscription1 { get; set; }
        public ParameterSubscription ParameterSubscription2 { get; set; }

        public ElementDefinition TestElementDefinition { get; set; }

        private Uri uri;
        private SiteDirectory siteDirectory;
        private TextParameterType parameterType;
        private TextParameterType parameterType2;
        private SimpleQuantityKind parameterType3;
        private EngineeringModel model;
        private EngineeringModelSetup engineeringModelSetup;
        private SiteReferenceDataLibrary siteReferenceDataLibrary;
        private ModelReferenceDataLibrary modelReferenceDataLibrary;
        private Person person;
        private Participant participant;
        private ElementDefinition elementDefinition3;
        private ElementDefinition elementDefinition2;
        private SimpleQuantityKind parameterType4;
        private ElementDefinition elementDefinition4;
        private SimpleQuantityKind parameterType7;
        private SimpleQuantityKind parameterType5;
        private SimpleQuantityKind parameterType6;

        [SetUp]
        public void Setup()
        {
            this.SetupData();
            this.RegisterMockSetup();
        }

        private void RegisterMockSetup()
        {
            this.Session.Setup(x => x.ActivePerson).Returns(this.person);
            this.Session.Setup(x => x.OpenIterations).Returns(new Dictionary<Iteration, Tuple<DomainOfExpertise, Participant>> { { this.Iteration, new Tuple<DomainOfExpertise, Participant>(this.Domain, this.participant) } });
            this.Session.Setup(x => x.Assembler).Returns(this.Assembler);
            this.Session.Setup(x => x.DataSourceUri).Returns(this.uri.ToString);

            this.SessionService.Setup(x => x.IsSessionOpen()).Returns(true);
            this.SessionService.Setup(x => x.Session).Returns(this.Session.Object);
            this.SessionService.Setup(x => x.Iteration).Returns(this.Iteration);
            this.SessionService.Setup(x => x.SiteDirectory).Returns(this.siteDirectory);
            this.SessionService.Setup(x => x.Transactions).Returns(this.Transactions);
            this.SessionService.Setup(x => x.DomainOfExpertise).Returns(this.Domain);
            this.SessionService.Setup(x => x.Cache).Returns(this.Session.Object.Assembler.Cache);

            this.FilterService.Setup(x => x.ProcessFilters(this.Iteration, this.siteDirectory.Domain));

            this.FilterService.Setup(x => x.IsFilteredIn(It.IsAny<ElementDefinition>())).Returns((Func<ElementDefinition, bool>) this.IsFilteredInMock);
            this.FilterService.Setup(x => x.IsFilteredInOrFilterIsEmpty(It.IsAny<ElementDefinition>())).Returns((Func<ElementDefinition, bool>) this.IsFilteredInOrFilterIsEmpty);
            this.FilterService.Setup(x => x.IsParameterSpecifiedOrAny(It.IsAny<Parameter>())).Returns((Func<Parameter, bool>) this.IsParameterSpecifiedOrAnyMock);
        }

        private bool IsFilteredInOrFilterIsEmpty(ElementDefinition element)
        {
            return string.IsNullOrWhiteSpace(this.CommandArguments.ElementDefinition) || this.CommandArguments.ElementDefinition == element.ShortName;
        }

        private bool IsFilteredInMock(ElementDefinition element)
        {
            return this.CommandArguments.ElementDefinition == element.ShortName;
        }

        private bool IsParameterSpecifiedOrAnyMock(Parameter element)
        {
            return this.CommandArguments.SelectedParameters.Any() || this.CommandArguments.SelectedParameters.Contains(element.ParameterType.ShortName);
        }

        internal virtual void BuildAction(string action)
        {
            var args = $"{this.baseArguments} {action}".Split(' ');

            Parser.Default.ParseArguments<Arguments>(args)
                .WithNotParsed(errors => { Assert.Fail($"Fail to parse arguments: {errors}"); })
                .WithParsed(arguments => this.CommandArguments = arguments);
        }

        private void SetupData()
        {
            this.Transactions.Clear();
            this.SessionService = new Mock<ISessionService>();
            this.FilterService = new Mock<IFilterService>();
            this.Session = new Mock<ISession>();
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

            this.engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "TEST" };
            this.engineeringModelSetup.IterationSetup.Add(iterationSetup);

            this.engineeringModelSetup.RequiredRdl.Add(this.modelReferenceDataLibrary);

            this.model.EngineeringModelSetup = this.engineeringModelSetup;
            this.model.EngineeringModelSetup.Participant.Add(this.participant);

            this.Assembler.Cache.TryAdd(new CacheKey(this.Iteration.Iid, null), new Lazy<Thing>(() => this.Iteration));

            this.AddThingsInCache();
        }

        private void AddThingsInCache()
        {
            foreach (var element in this.Iteration.Element)
            {
                this.Assembler.Cache.TryAdd(new CacheKey(element.Iid, this.Iteration.Iid), new Lazy<Thing>(() => element));

                foreach (var parameter in element.Parameter)
                {
                    this.Assembler.Cache.TryAdd(new CacheKey(parameter.Iid, this.Iteration.Iid), new Lazy<Thing>(() => parameter));
                    this.Assembler.Cache.TryAdd(new CacheKey(parameter.ParameterType.Iid, this.Iteration.Iid), new Lazy<Thing>(() => parameter.ParameterType));

                    foreach (var subscription in parameter.ParameterSubscription)
                    {
                        this.Assembler.Cache.TryAdd(new CacheKey(subscription.Iid, this.Iteration.Iid), new Lazy<Thing>(() => subscription));

                        foreach (var subscriptionValueSet in subscription.ValueSet)
                        {
                            this.Assembler.Cache.TryAdd(new CacheKey(subscriptionValueSet.Iid, this.Iteration.Iid), new Lazy<Thing>(() => subscriptionValueSet));
                        }
                    }

                    foreach (var valueSet in parameter.ValueSet)
                    {
                        this.Assembler.Cache.TryAdd(new CacheKey(valueSet.Iid, this.Iteration.Iid), new Lazy<Thing>(() => valueSet));
                    }
                }

                foreach (var elementUsage in element.ContainedElement)
                {
                    this.Assembler.Cache.TryAdd(new CacheKey(elementUsage.Iid, this.Iteration.Iid), new Lazy<Thing>(() => elementUsage));

                    foreach (var parameterOveride in elementUsage.ParameterOverride)
                    {
                        this.Assembler.Cache.TryAdd(new CacheKey(parameterOveride.Iid, this.Iteration.Iid), new Lazy<Thing>(() => parameterOveride));
                    }
                }
            }

            this.Assembler.Cache.TryAdd(new CacheKey(this.model.Iid, null), new Lazy<Thing>(() => this.model));
            this.Assembler.Cache.TryAdd(new CacheKey(this.siteReferenceDataLibrary.Iid, null), new Lazy<Thing>(() => this.siteReferenceDataLibrary));
            this.Assembler.Cache.TryAdd(new CacheKey(this.ActualPossibleFiniteStateList.Iid, null), new Lazy<Thing>(() => this.ActualPossibleFiniteStateList));

            foreach (var possibleFiniteState in this.ActualPossibleFiniteStateList.PossibleFiniteStateList.ToArray())
            {
                this.Assembler.Cache.TryAdd(new CacheKey(possibleFiniteState.Iid, this.Iteration.Iid), new Lazy<Thing>(() => possibleFiniteState));
            }
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

            this.Domain3 = new DomainOfExpertise(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "PWR", ShortName = "PWR" };
            this.siteDirectory.Domain.Add(this.Domain3);

            this.Domain4 = new DomainOfExpertise(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "SYS", ShortName = "SYS" };
            this.siteDirectory.Domain.Add(this.Domain4);

            this.Domain5 = new DomainOfExpertise(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Name = "CONF", ShortName = "CONF" };
            this.siteDirectory.Domain.Add(this.Domain5);

            this.person = new Person(Guid.NewGuid(), this.Assembler.Cache, this.uri);
            this.siteDirectory.Person.Add(this.person);

            this.participant = new Participant(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Person = this.person };
            this.participant.Domain.Add(this.Domain);
        }

        private void SetupElementDefinitionsAndUsages()
        {
            this.TestElementDefinition = new ElementDefinition(Guid.NewGuid(), this.Assembler.Cache, this.uri)
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

            this.elementDefinition4 = new ElementDefinition(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Name = "Generic Equipment",
                Owner = this.Domain,
                Container = this.Iteration,
                ShortName = "testElementDefinition4"
            };

            this.Iteration.Element.Add(this.TestElementDefinition);
            this.Iteration.Element.Add(this.elementDefinition2);
            this.Iteration.Element.Add(this.elementDefinition3);
            this.Iteration.Element.Add(this.elementDefinition4);

            this.SetupParameter();

            this.TestElementDefinition.Parameter.Add(this.Parameter);
            this.TestElementDefinition.Parameter.Add(this.Parameter2);
            this.TestElementDefinition.Parameter.Add(this.Parameter3);
            this.elementDefinition2.Parameter.Add(this.Parameter4);
            this.elementDefinition4.Parameter.Add(this.Parameter5);
            this.elementDefinition4.Parameter.Add(this.Parameter6);
            this.elementDefinition4.Parameter.Add(this.Parameter7);

            var parameterOverride = new ParameterOverride(Guid.NewGuid(), this.Assembler.Cache, this.uri) { Owner = this.Domain, Parameter = this.Parameter };

            var elementUsage = new ElementUsage(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ElementDefinition = this.TestElementDefinition, Owner = this.Domain};
            elementUsage.ParameterOverride.Add(parameterOverride);
            this.TestElementDefinition.ContainedElement.Add(elementUsage);
        }

        private void SetupParameter()
        {
            this.ValueSet = new ParameterValueSet
            {
                ValueSwitch = ParameterSwitchKind.REFERENCE,
                Reference = new ValueArray<string>(new List<string>() { "5555" }),
                Manual = new ValueArray<string>(new List<string>() { "-" }),
                Computed = new ValueArray<string>(new List<string>() { "-" }),
                Published = new ValueArray<string>(new List<string>() { "-" })
            };

            this.ParameterSubscription = new ParameterSubscription(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Owner = this.Domain,
                ValueSet =
                {
                    new ParameterSubscriptionValueSet(Guid.NewGuid(), this.Assembler.Cache, this.uri)
                    {
                        SubscribedValueSet = this.ValueSet
                    }
                }
            };

            this.ParameterSubscription1 = new ParameterSubscription(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Owner = this.Domain2,
                ValueSet =
                {
                    new ParameterSubscriptionValueSet(Guid.NewGuid(), this.Assembler.Cache, this.uri)
                    {
                        SubscribedValueSet = this.ValueSet,
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                        Manual = new ValueArray<string>(new List<string>() { "-" })
                    }
                }
            };

            this.ParameterSubscription2 = new ParameterSubscription(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Owner = this.Domain,
                ValueSet =
                {
                    new ParameterSubscriptionValueSet(Guid.NewGuid(), this.Assembler.Cache, this.uri)
                    {
                        SubscribedValueSet = this.ValueSet,
                        ValueSwitch = ParameterSwitchKind.MANUAL,
                        Manual = new ValueArray<string>(new List<string>() { "-" })
                    }
                }
            };

            this.parameterType = new TextParameterType(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter" };
            this.parameterType2 = new TextParameterType(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter2" };
            this.parameterType3 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "testParameter3" };

            this.parameterType4 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                ShortName = "l", PossibleScale = new List<MeasurementScale>()
                {
                    this.MeterScale, this.KilometerScale, this.MillimeterScale
                }
            };

            this.parameterType5 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "P_mean" };
            this.parameterType6 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "P_duty_cyc" };
            this.parameterType7 = new SimpleQuantityKind(Guid.NewGuid(), this.Assembler.Cache, this.uri) { ShortName = "loc" };

            this.Parameter = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.TestElementDefinition,
                ParameterType = this.parameterType,
                Owner = this.TestElementDefinition.Owner
            };

            this.Parameter2 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.TestElementDefinition,
                ParameterType = this.parameterType2,
                Owner = this.TestElementDefinition.Owner
            };

            this.Parameter3 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.TestElementDefinition,
                ParameterType = this.parameterType3,
                Owner = this.Domain2,
                IsOptionDependent = true,
                StateDependence = this.ActualPossibleFiniteStateList,
                Scale = this.MeterScale,
                ValueSet = { this.ValueSet },
                ParameterSubscription = { this.ParameterSubscription }
            };

            this.Parameter3s = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.TestElementDefinition,
                ParameterType = this.parameterType5,
                Owner = this.Domain2,
                ValueSet = { this.ValueSet },
                ParameterSubscription = { this.ParameterSubscription2 }
            };

            this.Parameter4 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition2,
                ParameterType = this.parameterType4,
                Owner = this.TestElementDefinition.Owner,
                Scale = this.MeterScale,
                ValueSet = { this.ValueSet },
                ParameterSubscription = { this.ParameterSubscription1 }
            };

            this.Parameter5 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition4,
                ParameterType = this.parameterType5,
                Owner = this.TestElementDefinition.Owner
            };

            this.Parameter6 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition4,
                ParameterType = this.parameterType6,
                Owner = this.TestElementDefinition.Owner
            };

            this.Parameter7 = new Parameter(Guid.NewGuid(), this.Assembler.Cache, this.uri)
            {
                Container = this.elementDefinition4,
                ParameterType = this.parameterType7,
                Owner = this.TestElementDefinition.Owner
            };

            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType2);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType3);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType4);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType5);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType6);
            this.siteReferenceDataLibrary.ParameterType.Add(this.parameterType7);
        }
    }
}
