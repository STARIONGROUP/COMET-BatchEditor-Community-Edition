//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DalSessionExtensions.cs" company="RHEA System S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using CDP4Dal;
    using CDP4Dal.Operations;

    /// <summary>
    /// Provides Extensions for the <see cref="CDP4Dal.Session" />
    /// </summary>
    public static class DalSessionExtensions
    {
        /// <summary>
        /// Finalizes the transaction and writes the session.
        /// </summary>
        /// <param name="session">The <see cref="ISession" /> to perform the write on.</param>
        /// <param name="transactions">The <see cref="IEnumerable{ThingTransactions}" />.</param>
        /// <returns></returns>
        public static async Task Write(this ISession session, IEnumerable<ThingTransaction> transactions)
        {
            try
            {
                foreach (var transaction in transactions)
                {
                    Console.Write("=");
                    var operationContainer = transaction.FinalizeTransaction();
                    await session.Write(operationContainer);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("The inline update operation failed", exception);
            }
        }
    }
}
