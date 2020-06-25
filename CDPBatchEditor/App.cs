//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="App.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor
{
    using System;
    using System.Diagnostics;

    using CDPBatchEditor.Commands.Interface;
    using CDPBatchEditor.Services.Interfaces;

    using NLog;
    using NLog.Config;
    using NLog.Targets;

    /// <summary>
    /// The application.
    /// </summary>
    public class App : IApp
    {
        /// <summary>
        /// Gets the injected <see cref="ICommandDispatcher" />
        /// </summary>
        private readonly ICommandDispatcher commandDispatcher;

        /// <summary>
        /// Gets the injected <see cref="ISessionService" />
        /// </summary>
        private readonly ISessionService sessionService;

        /// <summary>
        /// Instanciate a new <see cref="App" /> of Batch editor
        /// </summary>
        public App(ISessionService sessionService, ICommandDispatcher commandDispatcher)
        {
            // Setup NLog
            var config = new LoggingConfiguration();

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Error, LogLevel.Fatal, new ConsoleTarget("logConsole"));

            // Apply config
            LogManager.Configuration = config;

            this.sessionService = sessionService;
            this.commandDispatcher = commandDispatcher;
            this.StopWatch = new Stopwatch();
        }

        /// <summary>
        /// Gets a <see cref="Stopwatch" /> to measure time the tool took to complete
        /// </summary>
        public Stopwatch StopWatch { get; private set; }

        /// <summary>
        /// Runs this console app with the provided option
        /// </summary>
        public void Run()
        {
            this.StopWatch.Start();
            this.commandDispatcher.Invoke();
        }

        /// <summary>
        /// Closes the app by closing the <see cref="ISessionService.Session" /> and stop the <see cref="StopWatch" />
        /// </summary>
        public void Stop()
        {
            this.sessionService.CloseAndSave();

            this.StopWatch.Stop();
            Console.WriteLine($"BatchEditor completed in {this.StopWatch.ElapsedMilliseconds / 1000.0} s");
        }
    }
}
