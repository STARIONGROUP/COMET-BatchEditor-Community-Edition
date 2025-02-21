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
