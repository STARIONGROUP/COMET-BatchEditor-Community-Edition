//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ArgumentsBase.cs" company="RHEA System S.A.">
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
    using System.Collections.Generic;
    using System.Linq;

    using CommandLine;

    /// <summary>
    /// Generic command line arguments console applications .
    /// </summary>
    /// <remarks>
    /// For documentation see <see href="https://github.com/gsscoder/commandline/wiki/Quickstart" />.
    /// </remarks>
    public abstract class ArgumentsBase
    {
        /// <summary>
        /// Summarize the arguments set.
        /// </summary>
        /// <returns>
        /// A string representing the arguments in the form of name / value pairs.
        /// </returns>
        public override string ToString()
        {
            var optionValuePairs = new List<string>();

            foreach (var propertyInfo in this.GetType().GetProperties())
            {
                var optionAttributes = propertyInfo.GetCustomAttributes(typeof(OptionAttribute), true);

                if (optionAttributes.Length == 1)
                {
                    var longName = ((OptionAttribute) optionAttributes[0]).LongName;
                    var displayValue = FormatValue(propertyInfo.GetValue(this));

                    optionValuePairs.Add($"\n--{longName}=\"{displayValue}\"");
                }
            }

            return string.Join(" ", optionValuePairs.OrderBy(x => x));
        }

        /// <summary>
        /// Formats the argument set correctly.
        /// </summary>
        /// <param name="value">The argument as either one or a collection.</param>
        /// <returns>A string representing the formatted value.</returns>
        private static string FormatValue(object value)
        {
            var displayValue = value == null ? "" : value.ToString();

            if (!(value is string) && value is IEnumerable<string> collection)
            {
                displayValue = string.Join(",", collection);
            }

            return displayValue;
        }
    }
}
