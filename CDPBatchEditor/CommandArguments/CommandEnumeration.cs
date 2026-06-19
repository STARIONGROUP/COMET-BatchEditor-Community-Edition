//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="CommandEnumeration.cs" company="Starion Group S.A.">
//     Copyright (c) 2015-2024 Starion Group S.A.
//

//     This file is part of COMET Batch Editor.
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
        /// Add requirement SimpleParameterValues #
        /// </summary>
        AddRequirementParameters,

        /// <summary>
        /// Remove requirement SimpleParameterValues #
        /// </summary>
        RemoveRequirementParameters,

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
        /// Override parameters #
        /// </summary>
        Override,

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
        Subscribe,

        /// <summary>
        /// Copy or update <see cref="CDP4Common.EngineeringModelData.ElementDefinition" />s, their parameters,
        /// parameter values and parameter groups from a source engineering model into a target engineering model #
        /// <example>action=SyncElementDefinitions --source-model=SRC --target-model=TGT</example>
        /// </summary>
        SyncElementDefinitions,

        /// <summary>
        /// Synchronise (overwrite) every <see cref="CDP4Common.EngineeringModelData.ElementUsage" />'s short name and
        /// name with the short name and name of the <see cref="CDP4Common.EngineeringModelData.ElementDefinition" /> it
        /// references #
        /// <example>action=SyncElementUsageNames -m LOFT</example>
        /// </summary>
        SyncElementUsageNames
    }
}
