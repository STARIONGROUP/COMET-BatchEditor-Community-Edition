//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CsvFileWriter.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using NLog;

    /// <summary>
    /// Simple comma separated value file writer.
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        /// <summary>
        /// Reference to the active logger
        /// </summary>
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Stored field count of previous record.
        /// </summary>
        private int previousFieldCount;

        /// <summary>
        /// Initialises a new instance of the <see cref="CsvFileWriter" /> class.
        /// </summary>
        /// <param name="path">
        /// The file path.
        /// </param>
        public CsvFileWriter(string path) : base(path, false, Encoding.UTF8)
        {
            this.previousFieldCount = -1;
        }

        /// <summary>
        /// Write a list of field values as a single row to the CSV file.
        /// </summary>
        /// <param name="fields">
        /// The list of field values.
        /// </param>
        public void WriteRow(IEnumerable<string> fields)
        {
            var fieldCount = 0;
            var stringBuilder = new StringBuilder();

            foreach (var field in fields)
            {
                stringBuilder.Append('"');
                stringBuilder.Append(field?.Replace("\"", "\"\""));
                stringBuilder.Append('"');
                stringBuilder.Append(';');
                fieldCount++;
            }

            // Remove the last comma if present, and check the number of fields w.r.t. the previous record
            if (fieldCount > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            if (this.previousFieldCount >= 0 && fieldCount != this.previousFieldCount)
            {
                Logger.Warn("Number of CSV fields ({0}) differs from previous record ({1}): ({2})", fieldCount, this.previousFieldCount, stringBuilder);
            }

            this.previousFieldCount = fieldCount;

            // Write the CSV record
            this.WriteLine(stringBuilder.ToString());
        }
    }
}
