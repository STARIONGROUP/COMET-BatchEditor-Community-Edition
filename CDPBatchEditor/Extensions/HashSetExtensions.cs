//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="HashSetExtensions.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// <see cref="HashSetExtensions" /> provides helper collection extensions
    /// </summary>
    public static class HashSetExtensions
    {
        /// <summary>
        /// Allow to add a range of the <see cref="T" /> type to the collection
        /// </summary>
        /// <typeparam name="T">the type of the elements contained in the collection</typeparam>
        /// <param name="hashSet">the target collection</param>
        /// <param name="collection">the collection of elements to add to the target</param>
        /// <returns>true if all elements could have been added</returns>
        public static bool AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> collection)
        {
            return collection.Aggregate(true, (current, element) => current & hashSet.Add(element));
        }
    }
}
