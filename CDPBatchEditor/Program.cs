//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="RHEA System S.A.">
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
    using System.Linq;

    using Autofac;

    using CDPBatchEditor.CommandArguments;
    using CDPBatchEditor.Resources;

    using CommandLine;

    /// <summary>
    /// Top Container classe that runs the the BatchEditor
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method that is the entry point for this BatchEditor
        /// </summary>
        /// <param name="args">The arguments</param>
        public static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Arguments>(args).WithNotParsed(
                        errors =>
                        {
                            Console.WriteLine("Argument parsing error");

                            foreach (var error in errors.OrderBy(et => et.Tag))
                            {
                                Console.WriteLine($"***{error}***");
                            }
                        })
                    .WithParsed(
                        commandArgument =>
                        {
                            if (AppContainer.Container == null)
                            {
                                AppContainer.BuildContainer(commandArgument);
                            }

                            using var containerScope = AppContainer.Container?.BeginLifetimeScope();

                            Console.WriteLine(
                                containerScope.Resolve<IResourceLoader>()
                                    .LoadEmbeddedResource("CDPBatchEditor.Resources.ascii-art.txt")
                                    .Replace("BatchEditorVersion", QueryBatchEditorVersion()));

                            var app = containerScope.Resolve<IApp>();

                            try
                            {
                                app.Run();
                            }
                            finally
                            {
                                app.Stop();
                            }
                        });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        /// <summary>
        /// queries the version number from the executing assembly
        /// </summary>
        /// <returns>
        /// a string representation of the version of the application
        /// </returns>
        public static string QueryBatchEditorVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetName().Version.ToString();
        }
    }
}
