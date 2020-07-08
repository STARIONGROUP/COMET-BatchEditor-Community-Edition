//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="AppTestFixture.cs" company="RHEA System S.A.">
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
    using CDPBatchEditor.Commands.Interface;
    using CDPBatchEditor.Services.Interfaces;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class AppTestFixture
    {
        [SetUp]
        public void Setup()
        {
            this.commandDispatcher = new Mock<ICommandDispatcher>();
            this.sessionService = new Mock<ISessionService>();
            this.app = new App(this.sessionService.Object, this.commandDispatcher.Object);
        }

        private App app;
        private Mock<ICommandDispatcher> commandDispatcher;
        private Mock<ISessionService> sessionService;

        [Test]
        public void VerifyRun()
        {
            this.app.Run();
            this.commandDispatcher.Verify(x => x.Invoke(), Times.Once);
            Assert.IsTrue(this.app.StopWatch.IsRunning);
        }

        [Test]
        public void VerifyStop()
        {
            this.app.Stop();
            this.sessionService.Verify(x => x.CloseAndSave(), Times.Once);
            Assert.IsFalse(this.app.StopWatch.IsRunning);
        }
    }
}
