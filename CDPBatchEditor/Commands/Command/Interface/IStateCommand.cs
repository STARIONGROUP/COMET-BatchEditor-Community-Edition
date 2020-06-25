//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IStateCommand.cs" company="RHEA System S.A.">
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
    /// Defines an <see cref="IStateCommand" /> that Provides actions that are
    /// <see cref="CDP4Common.EngineeringModelData.PossibleFiniteState" /> and
    /// <see cref="CDP4Common.EngineeringModelData.ActualFiniteState" /> related
    /// </summary>
    public interface IStateCommand
    {
        /// <summary>
        /// Apply option dependence or remove it to/from selected parameters and overrides for element definitions and usages of
        /// selected categories.
        /// </summary>
        /// <param name="isStateDependencyToBeRemoved">the value whether the option dependency is to be removed</param>
        void ApplyOrRemoveStateDependency(bool isStateDependencyToBeRemoved);
    }
}
