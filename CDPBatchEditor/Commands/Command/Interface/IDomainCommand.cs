//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IDomainCommand.cs" company="RHEA System S.A.">
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

namespace CDPBatchEditor.Commands.Command.Interface
{
    /// <summary>
    /// Defines an <see cref="IDomainCommand" /> that provides actions that are
    /// <see cref="CDP4Common.SiteDirectoryData.DomainOfExpertise" /> related
    /// </summary>
    public interface IDomainCommand
    {
        /// <summary>
        /// Changes ownership owned items from one <see cref="CDP4Common.SiteDirectoryData.DomainOfExpertise" /> to another.
        /// </summary>
        void ChangeDomain();

        /// <summary>
        /// Set the ownership of all parameters inside element definitions that have a name starting with "Generic Equipment".
        /// </summary>
        void SetGenericEquipmentOwnership();

        /// <summary>
        /// Set the ownership of all parameters provided to the provided owner
        /// </summary>
        void ChangeParameterOwnership();
    }
}
