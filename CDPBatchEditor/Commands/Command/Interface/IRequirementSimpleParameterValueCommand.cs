//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ISimpleParameterValueCommand.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2025 Starion Group S.A.
//
//     This file is part of CDP4-COMET Batch Editor.
//     The CDP4-COMET Batch Editor is a commandline application to perform batch operations on a
//     ECSS-E-TM-10-25 Annex A and Annex C data source
//
//     The CDP4-COMET Batch Editor is free software; you can redistribute it and/or
//     modify it under the terms of the GNU Lesser General Public
//     License as published by the Free Software Foundation; either
//     version 3 of the License, or any later version.
//
//     The CDP4-COMET Batch Editor is distributed in the hope that it will be useful,
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
    using CDP4Common.EngineeringModelData;

    /// <summary>
    /// Defines an <see cref="IRequirementSimpleParameterValueCommand" /> that provides actions that are <see cref="SimpleParameterizableThing" /> related
    /// </summary>
    public interface IRequirementSimpleParameterValueCommand
    {
        /// <summary>
        /// Add one or more parameter to the specified <see cref="Requirement" /> into the specified group if provided
        /// </summary>
        void Add();

        /// <summary>
        /// Remove one or more parameter from the specified <see cref="Requirement" />
        /// </summary>
        void Remove();
    }
}
