# CDPBatchEditor Commands example
    
## Add a parameter

    -s http://localhost:5000 -u admin -p pass --action AddParameters -m TestEngineeringModelSetup --parameters TestSimpleQuantityKind --element-definition TestElementDefinition --domain TST
    
    -s http://localhost:5000 -u admin -p pass --action AddParameters -m LOFT --parameters n_items --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS

## Add a parameter and add it to group of parameter

    -s http://localhost:5000 -u admin -p pass --action AddParameters -m TestEngineeringModelSetup --parameters TestBooleanParameterType --element-definition TestElementDefinition --domain TST --parameter-group TestGroup

    -s http://localhost:5000 -u admin -p pass --action AddParameters --parameter-group TestGroup -m LOFT --parameters n_items --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS

## Remove a parameter

    -s http://localhost:5000 -u admin -p pass --action RemoveParameters -m TestEngineeringModelSetup --parameters TestTextParameterType --element-definition TestElementDefinition --domain TST

    -s http://localhost:5000 -u admin -p pass --action RemoveParameters -m LOFT --parameters n_items --element-definition a10_layers_MLI_on_tower --domain THE

## Subscribe to a parameter

    -s http://localhost:5000 -u admin -p pass --action Subscribe -m TestEngineeringModelSetup --parameters TestTextParameterType --element-definition TestElementDefinition --domain TST

    -s http://localhost:5000 -u admin -p pass --action Subscribe -m LOFT --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain SYS
    -s http://localhost:5000 -u admin -p pass --action Subscribe -m LOFT --parameters n_items --element-definition a1mil_layer_kapton_on_BEE_boxes --domain THE

## Override a parameter

    -s http://localhost:5000 -u admin -p pass --action Override -m TestEngineeringModelSetup --parameters TestTextParameterType --element-definition TestElementDefinition 

    -s http://localhost:5000 -u admin -p pass --action Override -m LOFT --parameters mass_margin --element-definition a10_layers_MLI_on_tower 
    -s http://localhost:5000 -u admin -p pass --action Override -m LOFT --parameters n_items --include-owners SYS

## Move reference values to manual values

    -s http://localhost:5000 -u admin -p pass --action MoveReferenceValuesToManualValues -m TestEngineeringModelSetup --parameters TestTextParameterType --element-definition TestElementDefinition --domain TST

    -s http://localhost:5000 -u admin -p pass --action MoveReferenceValuesToManualValues -m LOFT --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain THE

## Apply Option Dependency

    -s http://localhost:5000 -u admin -p pass --action ApplyOptionDependence -m LOFT --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain THE

## Remove Option Dependency

    -s http://localhost:5000 -u admin -p pass --action RemoveOptionDependence -m LOFT --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain THE

## Apply State Dependency

    -s http://localhost:5000 -u admin -p pass --action ApplyStateDependence -m LOFT --state power --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain THE

## Remove State Dependency

    -s http://localhost:5000 -u admin -p pass --action RemoveStateDependence -m LOFT --state power --parameters mass_margin --element-definition a10_layers_MLI_on_tower --domain THE

## Change Parameter ownership

    -s http://localhost:5000 -u admin -p pass --action ChangeParameterOwnership -m LOFT --parameters mass_margin,m,l --element-definition a10_layers_MLI_on_tower --domain THE

    -s http://localhost:5000 -u admin -p pass --action ChangeParameterOwnership -m LOFT --parameters mass_margin,m,l --domain SYS

## Change owner

    -s http://localhost:5000 -u admin -p pass --action ChangeDomain -m LOFT --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS --to-domain THE

    -s http://localhost:5000 -u admin -p pass --action ChangeDomain -m LOFT --parameters m,l,h --element-definition a1mil_layer_kapton_on_BEE_boxes --domain THE --to-domain SYS

## Set scale

    -s http://localhost:5000 -u admin -p pass --action SetScale -m LOFT --scale μm --parameters m,l,h --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS

## Set scale to mm

    -s http://localhost:5000 -u admin -p pass --action StandardizeDimensionsInMillimeter -m LOFT --element-definition a1mil_layer_kapton_on_BEE_boxes --domain SYS
    -s http://localhost:5000 -u admin -p pass --action StandardizeDimensionsInMillimeter -m LOFT --domain SYS

## SetSubscriptionSwitch

    -s http://localhost:5000 -u admin -p pass --action SetSubscriptionSwitch --parameter-switch COMPUTED -m LOFT --parameters l --element-definition a10_layers_MLI_on_tower --domain SYS

## AddRequirementParameter
    -s http://localhost:5000 -u admin -p pass --action AddRequirementParameters -m LOFT --parameters l --requirements-specification ReqSpec1 --domain SYS

## RemoveRequirementParameter
    -s http://localhost:5000 -u admin -p pass --action RemoveRequirementParameters -m LOFT --parameters l --requirements-specification ReqSpec1 --domain SYS

## SyncElementDefinitions (copy or update)

Copies or updates ElementDefinitions, their Parameters, parameter values and parameter groups from a source model
(`--source-model`) into a target model (`--target-model`). ElementDefinitions are matched by ShortName; a ShortName
that occurs more than once in either model is skipped. The source published value is written as the target reference
value; the value switch is set to REFERENCE only when a parameter is first copied (never changed on an update), and an
existing value is overwritten only when the source published value differs and is not `-`. Owners are written only when
a new ElementDefinition or Parameter is created (never updated). Only ParameterTypes and Categories reachable through the
target model's chain of reference data libraries are copied. Nothing is written or reported for things that did not
actually change. A `LastSyncReport.txt` file (overwritten each run) lists every copied/updated ElementDefinition, the
parameter value changes, and any exclusions or skips. `-m/--model` is not used for this action.

    -s http://localhost:5000 -u admin -p pass --action SyncElementDefinitions --source-model SRC --target-model TGT

Restrict to certain categories and parameters, and preview with a dry run:

    -s http://localhost:5000 -u admin -p pass --action SyncElementDefinitions --source-model SRC --target-model TGT --categories equipment --parameters mass,power --dry

Also pull in child Element Usages (and their referenced Element Definitions and Parameter Overrides) when the usage is a
member of one of the element usage categories. A usage qualifies on its effective categories (its own categories combined
with those of its referenced Element Definition). This descends recursively through the decomposition, so a nested
A -> B -> C tree is reproduced with each usage nested under the correct parent.

    -s http://localhost:5000 -u admin -p pass --action SyncElementDefinitions --source-model SRC --target-model TGT --categories equipment --element-usage-categories battery,sensor

Parameter Groups are flattened and matched by Name within the Element Definition: only the top-level (root) group of each
copied parameter is copied to the target, and a parameter nested in a source group A/B/C is placed in the top-level group
A. Parameters (new and existing) are re-linked to the correct top-level group. By default empty target groups are left in
place; add `--prune-groups` to also delete target groups that are not among the copied root groups and end up empty.

    -s http://localhost:5000 -u admin -p pass --action SyncElementDefinitions --source-model SRC --target-model TGT --prune-groups
