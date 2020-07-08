//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ProgramTestFixture.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Tests
{
    using System;

    using Autofac;

    using CDPBatchEditor.CommandArguments.Interface;
    using CDPBatchEditor.Commands.Interface;
    using CDPBatchEditor.Resources;
    using CDPBatchEditor.Services.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class ProgramTestFixture
    {
        [SetUp]
        public void Setup()
        {
            this.app = new Mock<IApp>();
            this.resourceLoader = new Mock<IResourceLoader>();
            this.commandArguments = new Mock<ICommandArguments>();

            this.resourceLoader.Setup(x => x.LoadEmbeddedResource(It.IsAny<string>())).Returns("Test passes ok");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterInstance(this.app.Object).As<IApp>();
            containerBuilder.RegisterInstance(this.resourceLoader.Object).As<IResourceLoader>();
            AppContainer.Container = containerBuilder.Build();
        }

        private const string SampleArguments = "-s http://localhost:5000 -u admin -p pass --action ChangeDomain -m LOFT --parameters m,l,h --element-definition a1mil_layer_kapton_on_BEE_boxes --domain THE --to-domain SYS";
        private const string WrongSampleArguments = "-w http://localhost:5000 -u admin -p pass --action ChangeDomain -m LOFT --parameters m,l,h --element-definition a1mil_layer_kapton_on_BEE_boxes --domain THE --to-domain SYS";
        private Mock<IApp> app;
        private Mock<IResourceLoader> resourceLoader;
        private Mock<ICommandArguments> commandArguments;

        private void VerifyCalls(Times times)
        {
            this.resourceLoader.Verify(x => x.LoadEmbeddedResource(It.IsAny<string>()), times);
            this.app.Verify(x => x.Run(), times);
            this.app.Verify(x => x.Stop(), times);
        }

        [Test]
        public void VerifyAssemblyVersion()
        {
            var version = Program.QueryBatchEditorVersion();
            Assert.IsNotNull(version);
            Assert.AreEqual(4, version.Split('.').Length);
            Assert.IsNotNull(new Version(version));
        }

        [Test]
        public void VerifyContainer()
        {
            AppContainer.BuildContainer(this.commandArguments.Object);
            Assert.IsNotNull(AppContainer.Container);
            Assert.IsNotEmpty(AppContainer.Container.ComponentRegistry.Registrations);
            Assert.IsTrue(AppContainer.Container.IsRegistered<IApp>());
            Assert.IsTrue(AppContainer.Container.IsRegistered<ISessionService>());
            Assert.IsTrue(AppContainer.Container.IsRegistered<ICommandArguments>());
            Assert.IsTrue(AppContainer.Container.IsRegistered<ICommandDispatcher>());
        }

        [Test]
        public void VerifyMain()
        {
            var arguments = WrongSampleArguments.Split(' ');
            Program.Main(arguments);
            this.VerifyCalls(Times.Never());

            arguments = SampleArguments.Split(' ');
            Program.Main(arguments);
            this.VerifyCalls(Times.Once());
        }
    }
}
