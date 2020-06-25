//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ConnectionArguments.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.CommandArguments
{
    using System;

    using CommandLine;

    /// <summary>
    /// Generic command line options for console applications that use an engineering model as input.
    /// </summary>
    /// <remarks>
    /// For documentation see <see href="https://github.com/gsscoder/commandline/wiki/Quickstart" />.
    /// </remarks>
    public abstract class ConnectionArguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the server URI string.
        /// </summary>
        [Option('s', "server", Default = "https://cdp4services-test.cdp4.org", Required = true, HelpText = "Uri of CDP server to connect to.")]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// Gets or sets the short name of the engineering model to edit.
        /// <code>shortname = 'm' longName = 'model'</code>
        /// </summary>
        [Option('m', "model", Required = true, HelpText = "Short name of the engineering model to process. Asterisk means: process all engineering models.")]
        public string EngineeringModel { get; set; }

        /// <summary>
        /// Gets or sets the user name
        /// <code>shortname = 'u' longName = 'user'</code>
        /// </summary>
        [Option('u', "user", Default = "admin", Required = true, HelpText = "Username to connect with")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password
        /// <code>shortname = 'p' longName = 'password'</code>
        /// </summary>
        [Option('p', "password", Default = "pass", Required = true, HelpText = "Password associated to the username to connect with")]
        public string Password { get; set; }
    }
}
