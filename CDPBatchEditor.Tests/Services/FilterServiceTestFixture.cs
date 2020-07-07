// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterServiceTestFixture.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Tests.Services
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Services;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class FilterServiceTestFixture
    {
        private const string BaseUri = "http://test.com";
        private Mock<ICommandArguments> commandArguments;
        private FilterService filterService;
        private Uri uri;
        private Assembler assembler;
        private SiteDirectory siteDirectory;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private ElementDefinition elementDefinition;
        private ElementDefinition elementDefinition2;
        private DomainOfExpertise domain3;
        private DomainOfExpertise domain2;
        private Parameter parameter2;
        private Parameter parameter;

        [SetUp]
        public void Setup()
        {
            this.commandArguments = new Mock<ICommandArguments>();

            this.uri = new Uri(BaseUri);
            this.assembler = new Assembler(this.uri);

            this.iteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri);

            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri);

            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "testDomain" };
            this.siteDirectory.Domain.Add(this.domain);
            this.domain2 = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "testDomain2" };
            this.siteDirectory.Domain.Add(this.domain2);
            this.domain3 = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "testDomain3" };
            this.siteDirectory.Domain.Add(this.domain3);

            this.elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "e", Owner = this.domain };
            this.elementDefinition2 = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "e2", Owner = this.domain };

            this.elementDefinition.ContainedElement.Add(new ElementUsage(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "e2u", Owner = this.domain, ElementDefinition = this.elementDefinition2});
            var parameterType = new DateTimeParameterType(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "t" };
            var parameterType2 = new SimpleQuantityKind(Guid.NewGuid(), this.assembler.Cache, this.uri) { ShortName = "o" };
            this.parameter = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri) { ParameterType = parameterType };
            this.parameter2 = new Parameter(Guid.NewGuid(), this.assembler.Cache, this.uri) { ParameterType = parameterType };
            this.iteration.Element.Add(this.elementDefinition);
            this.iteration.Element.Add(this.elementDefinition2);

            this.commandArguments.Setup(x => x.ElementDefinition).Returns(this.elementDefinition.ShortName);
            this.commandArguments.Setup(x => x.DomainOfExpertise).Returns(this.domain.ShortName);
            this.commandArguments.Setup(x => x.FilteredCategories).Returns(new List<string>());

            this.commandArguments.Setup(x => x.IncludedOwners).Returns(
                new List<string>()
                {
                    this.domain.ShortName, this.domain2.ShortName, this.domain3.ShortName
                });

            this.commandArguments.Setup(x => x.ExcludedOwners).Returns(
                new List<string>()
                {
                    this.domain3.ShortName
                });

            this.commandArguments.Setup(x => x.SelectedParameters).Returns(
                new List<string>()
                {
                    parameterType.ShortName, parameterType2.ShortName
                });

            this.filterService = new FilterService(this.commandArguments.Object);
        }

        [Test]
        public void VerifyIsParameterSpecifiedOrAny()
        {
            Assert.IsFalse(this.filterService.IsParameterSpecifiedOrAny(new Parameter() { ParameterType = new BooleanParameterType() { ShortName = "returnFalse" } }));

            Assert.IsTrue(this.filterService.IsParameterSpecifiedOrAny(this.parameter));

            this.commandArguments.Setup(x => x.SelectedParameters).Returns(new List<string>());

            Assert.IsEmpty(this.commandArguments.Object.SelectedParameters);
            Assert.IsTrue(this.filterService.IsParameterSpecifiedOrAny(this.parameter));
        }

        [Test]
        public void VerifyIsFilteredInOrFilterIsEmpty()
        {
            var dummyElement = new ElementDefinition() { ShortName = "null" };
            this.filterService.ProcessFilters(this.iteration, this.siteDirectory.Domain);
            Assert.IsTrue(this.filterService.IsFilteredInOrFilterIsEmpty(this.elementDefinition));

            Assert.IsFalse(this.filterService.IsFilteredInOrFilterIsEmpty(dummyElement));
            this.filterService.FilteredElementDefinitions.Clear();
            Assert.IsTrue(this.filterService.IsFilteredInOrFilterIsEmpty(dummyElement));
        }

        [Test]
        public void VerifyProcessFilters()
        {
            Assert.IsEmpty(this.filterService.FilteredElementDefinitions);
            Assert.IsEmpty(this.filterService.IncludedOwners);
            this.filterService.ProcessFilters(this.iteration, this.siteDirectory.Domain);
            Assert.IsNotEmpty(this.filterService.FilteredElementDefinitions);
            Assert.AreEqual(2, this.filterService.IncludedOwners.Count);
        }
    }
}
