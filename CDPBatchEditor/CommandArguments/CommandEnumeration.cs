//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CommandEnumeration.cs" company="RHEA System S.A.">
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
    /// <summary>
    /// Provides enumeration of the action available to apply with this BatchEditor tool
    /// </summary>
    public enum CommandEnumeration
    {
        /// <summary>
        /// Default value, it doesn't do anything #
        /// </summary>
        Unspecified,

        /// <summary>
        /// Add parameter #
        /// </summary>
        AddParameters,

        /// <summary>
        /// Remove parameters #
        /// </summary>
        RemoveParameters,

        /// <summary>
        /// Reference the manual value on a value set #
        /// </summary>
        MoveReferenceValuesToManualValues,

        /// <summary>
        /// Add an option dependency #
        /// </summary>
        ApplyOptionDependence,

        /// <summary>
        /// Apply a state dependency #
        /// </summary>
        ApplyStateDependence,

        /// <summary>
        /// Change the domain of expertise ownership on a parameter #
        /// </summary>
        ChangeParameterOwnership,

        /// <summary>
        /// Switch between domain of expertise #
        /// </summary>
        ChangeDomain,

        /// <summary>
        /// Remove option dependency #
        /// </summary>
        RemoveOptionDependence,

        /// <summary>
        /// Remove state dependency #
        /// </summary>
        RemoveStateDependence,

        /// <summary>
        /// Set the generic owners
        /// </summary>
        SetGenericOwners,

        /// <summary>
        /// Set the scale #
        /// </summary>
        SetScale,

        /// <summary>
        /// Set the shape scale to milimeters #
        /// </summary>
        StandardizeDimensionsInMillimeter,

        /// <summary>
        /// Set the subscrition switch #
        /// </summary>
        SetSubscriptionSwitch,

        /// <summary>
        /// Subscribe to parameters #
        /// <example>action=Subscribe --parameters=height,length,mass --domain=Thermal</example>
        /// </summary>
        Subscribe
    }
}
