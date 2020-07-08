//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SessionServiceTestFixture.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Tests.Services
{
    using System;
    using System.Collections.Generic;

    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Services;
    using CDPBatchEditor.Services.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class SessionServiceTestFixture
    {
        [SetUp]
        public void Setup()
        {
            this.commandArguments = new Mock<ICommandArguments>();
            this.filterService = new Mock<IFilterService>();
            this.session = new Mock<ISession>();
            this.uri = new Uri(BaseUri);
            this.assembler = new Assembler(this.uri);
            this.siteDirectory = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, this.uri);

            var iterationSetup = new IterationSetup(Guid.NewGuid(), this.assembler.Cache, this.uri);

            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.assembler.Cache, this.uri) { Name = "testDomain", ShortName = "testDomain" };
            this.siteDirectory.Domain.Add(this.domain);
            this.person = new Person(Guid.NewGuid(), this.assembler.Cache, this.uri);
            this.siteDirectory.Person.Add(this.person);
            this.participant = new Participant(Guid.NewGuid(), this.assembler.Cache, this.uri) { Person = this.person };
            this.participant.Domain.Add(this.domain);

            var engineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, this.uri)
                { IterationSetup = { iterationSetup }, ActiveDomain = { this.domain } };

            this.engineeringModel = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, this.uri)
                { EngineeringModelSetup = engineeringModelSetup };

            this.iteration = new Iteration(Guid.NewGuid(), this.assembler.Cache, this.uri)
                { IterationSetup = iterationSetup };

            this.engineeringModel.Iteration.Add(this.iteration);
            this.siteDirectory.Model.Add(engineeringModelSetup);

            this.session.Setup(x => x.OpenIterations).Returns(new Dictionary<Iteration, Tuple<DomainOfExpertise, Participant>> { { this.iteration, new Tuple<DomainOfExpertise, Participant>(this.domain, this.participant) } });
            this.session.Setup(x => x.Assembler).Returns(this.assembler);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.ToString);
            this.session.Setup(x => x.RetrieveSiteDirectory()).Returns(this.siteDirectory);
            this.session.Setup(x => x.Close());
            this.session.Setup(x => x.Read(this.iteration, this.domain));

            this.filterService.Setup(x => x.ProcessFilters(this.iteration, this.siteDirectory.Domain));

            this.commandArguments.Setup(x => x.ServerUri).Returns(this.uri);
            this.commandArguments.Setup(x => x.DomainOfExpertise).Returns(this.domain.ShortName);
            this.commandArguments.Setup(x => x.Password).Returns("pass");
            this.commandArguments.Setup(x => x.UserName).Returns("admin");
            this.commandArguments.Setup(x => x.DryRun).Returns(false);

            this.sessionService = new SessionService(this.commandArguments.Object, this.filterService.Object, this.session.Object);
        }

        private const string BaseUri = "http://test.com";
        private Mock<ISession> session;
        private Uri uri;
        private SiteDirectory siteDirectory;
        private Iteration iteration;
        private DomainOfExpertise domain;
        private Person person;
        private Participant participant;
        private Assembler assembler;
        private SessionService sessionService;
        private Mock<IFilterService> filterService;
        private Mock<ICommandArguments> commandArguments;
        private EngineeringModel engineeringModel;

        private ThingTransaction CreateDummyTransactions()
        {
            var elementDefinition = new ElementDefinition(Guid.NewGuid(), this.assembler.Cache, this.uri);

            this.iteration.Element.Add(elementDefinition);

            var transactionContext = TransactionContextResolver.ResolveContext(this.iteration);
            return new ThingTransaction(transactionContext, elementDefinition);
        }

        [Test]
        public void VerifyOpen()
        {
            this.session.Setup(x => x.Open());
            this.sessionService.Open();
            this.session.Verify(x => x.Open(), Times.Once);
        }

        [Test]
        public void VerifySaveClose()
        {
            this.sessionService.Transactions.Add(this.CreateDummyTransactions());
            this.sessionService.Transactions.Add(this.CreateDummyTransactions());
            this.sessionService.Transactions.Add(this.CreateDummyTransactions());

            Assert.AreEqual(3, this.sessionService.Transactions.Count);

            this.sessionService.CloseAndSave();
            this.sessionService.Transactions.Clear();
            this.sessionService.Save();
            this.session.Verify(x => x.Write(It.IsAny<OperationContainer>()), Times.Exactly(3));

            this.session.Setup(x => x.RetrieveSiteDirectory());
            Assert.IsFalse(this.sessionService.IsSessionOpen());
            this.sessionService.Close();
        }

        [Test]
        public void VerifySessionOpen()
        {
            Assert.IsTrue(this.sessionService.IsSessionOpen());
        }

        [Test]
        public void VerifySetProperties()
        {
            Assert.IsTrue(this.sessionService.SetProperties());
        }
    }
}
