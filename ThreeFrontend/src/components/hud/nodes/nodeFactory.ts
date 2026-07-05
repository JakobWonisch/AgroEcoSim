import { NumberInputNode } from "./input/NumberInputNode";
import { BooleanInputNode } from "./input/BooleanInputNode";
import { ConfigurationValueInputNode } from "./input/ConfigurationValueInputNode";
import { AgentTypeInputNode } from "./input/AgentTypeInputNode";
import { ActiveOutputNode } from "./output/ActiveOutputNode";
import { GrowthNode } from "./output/GrowthNode";
import { PhaseInputNode } from "./input/PhaseInputNode";
import { AgentStateInputNode } from "./input/AgentStateInputNode";
import { FormationInputNode } from "./input/FormationInputNode";
import { IrradianceInputNode } from "./input/IrradianceInputNode";
import { SimulationSettingsInputNode } from "./input/SimulationSettingsInputNode";
import { RandomChanceInputNode } from "./input/RandomChanceInputNode";
import { RandomAccumChanceInputNode } from "./input/RandomAccumChanceInputNode";
import { RandomFloatVarInputNode } from "./input/RandomFloatVarInputNode";
import { AndNode } from "./util/boolean/AndNode";
import { NotNode } from "./util/boolean/NotNode";
import { OrNode } from "./util/boolean/OrNode";
import { XorNode } from "./util/boolean/XorNode";
import { EqualToNode } from "./util/logic/EqualToNode";
import { GreaterThanNode } from "./util/logic/GreaterThanNode";
import { GreaterThanOrEqualNode } from "./util/logic/GreaterThanOrEqualNode";
import { IfElseNode } from "./util/logic/IfElseNode";
import { LessThanNode } from "./util/logic/LessThanNode";
import { LessThanOrEqualNode } from "./util/logic/LessThanOrEqualNode";
import { AddNode } from "./util/numeric/AddNode";
import { DivideNode } from "./util/numeric/DivideNode";
import { MultiplyNode } from "./util/numeric/MultiplyNode";
import { SubtractNode } from "./util/numeric/SubtractNode";
import { ParentWoodCapNode } from "./util/numeric/ParentWoodCapNode";
import { ClampMaxNode } from "./util/numeric/ClampMaxNode";
import { DeltaEnergyNode } from "./output/DeltaEnergyNode";
import { DeltaWaterNode } from "./output/DeltaWaterNode";
import { DeltaWoodNode } from "./output/DeltaWoodNode";
import { SetWoodNode } from "./output/SetWoodNode";
import { MultiplyEnergyNode } from "./output/MultiplyEnergyNode";
import { MultiplyWaterNode } from "./output/MultiplyWaterNode";
import { SetEnergyNode } from "./output/SetEnergyNode";
import { SetAuxinsNode } from "./output/SetAuxinsNode";
import { SetTrySpawnNode } from "./output/SetTrySpawnNode";
import { AccumulateProductionNode } from "./output/AccumulateProductionNode";
import { AccumulateEnvResourcesNode } from "./output/AccumulateEnvResourcesNode";
import { AccumulateEnvResourcesInvNode } from "./output/AccumulateEnvResourcesInvNode";
import { MakeBudNode } from "./output/MakeBudNode";
import { CreateLeavesNode } from "./output/CreateLeavesNode";
import { DeathNode } from "./output/DeathNode";
import { DeathParentNode } from "./output/DeathParentNode";
import { DeathChildrenNode } from "./output/DeathChildrenNode";
import { BecomeMeristemNode } from "./output/BecomeMeristemNode";
import { SetLateralAngleNode } from "./output/SetLateralAngleNode";
import { DeltaDominanceNode } from "./output/DeltaDominanceNode";
import { SetLengthVarNode } from "./output/SetLengthVarNode";
import { TurnUpwardsNode } from "./output/TurnUpwardsNode";
import { SetWasMeristemNode } from "./output/SetWasMeristemNode";
import { BecomeStemNode } from "./output/BecomeStemNode";
import { BecomeFlowerStemNode } from "./output/BecomeFlowerStemNode";
import { BecomeFlowerMeristemNode } from "./output/BecomeFlowerMeristemNode";
import { applyNodeComment } from "./nodeComment";
import {
    SpawnMeristemNode,
    SpawnBudNode,
    SpawnStemNode,
    SpawnFlowerStemNode,
    SpawnFlowerMeristemNode,
    SpawnFlowerBudNode,
    SpawnFlowerPadelNode,
    SpawnRhizomeNode,
} from "./output/spawn/SpawnNodes";

/** Labels that match `super('…')` on node classes under hud/nodes (canonical export labels). */
export const canonicalBehaviorNodeLabels = [
    "Number Input",
    "Boolean Input",
    "Configuration Value Input",
    "Agent Type Input",
    "Phase Input",
    "Agent State Input",
    "Formation Input",
    "Irradiance Input",
    "Simulation Settings Input",
    "Random Chance Input",
    "Random Accum Chance Input",
    "Random Float Var Input",
    "Active",
    "And",
    "Or",
    "Xor",
    "Not",
    "Greater Than",
    "Greater Than or Equal",
    "Less Than",
    "Less Than or Equal",
    "Equal To",
    "If / Else",
    "Add",
    "Subtract",
    "Multiply",
    "Divide",
    "Parent Wood Cap",
    "Clamp Max",
    "Growth",
    "Delta Energy",
    "Delta Water",
    "Delta Wood",
    "Set Wood",
    "Multiply Energy",
    "Multiply Water",
    "Set Energy",
    "Set Auxins",
    "Set trySpawn",
    "Accumulate Production",
    "Accumulate Env Resources",
    "Accumulate Env Resources Inv",
    "Make Bud",
    "Create Leaves",
    "Death",
    "Death Parent",
    "Death Children",
    "Become Meristem",
    "Set Lateral Angle",
    "Delta Dominance",
    "Set Length Var",
    "Turn Upwards",
    "Set Was Meristem",
    "Become Stem",
    "Become Flower Stem",
    "Become Flower Meristem",
    "Spawn Meristem",
    "Spawn Bud",
    "Spawn Stem",
    "Spawn Flower Stem",
    "Spawn Flower Meristem",
    "Spawn Flower Bud",
    "Spawn Flower Padel",
    "Spawn Rhizome",
] as const;

type Creator = (data: Record<string, unknown>) => any;

function numberFromData(d: Record<string, unknown>): number {
    if (typeof d.value === "number") return d.value;
    if (typeof d.num === "number") return d.num;
    return 0;
}

const creators: Record<string, Creator> = {
    "Number Input": (d) => new NumberInputNode(numberFromData(d)),
    "Boolean Input": (d) => new BooleanInputNode(typeof d.bool === "boolean" ? d.bool : false),
    "Configuration Value Input": (d) => {
        const configId = typeof d.configId === "string" ? d.configId : "";
        const configType = d.configType === "boolean" ? "boolean" : "number";
        const node = new ConfigurationValueInputNode(configId, configType);
        if (configId) node.configId = configId;
        return node;
    },
    "Agent Type Input": () => new AgentTypeInputNode(),
    "Phase Input": () => new PhaseInputNode(),
    "Agent State Input": () => new AgentStateInputNode(),
    "Formation Input": () => new FormationInputNode(),
    "Irradiance Input": () => new IrradianceInputNode(),
    "Simulation Settings Input": () => new SimulationSettingsInputNode(),
    "Random Chance Input": () => new RandomChanceInputNode(),
    "Random Accum Chance Input": () => new RandomAccumChanceInputNode(),
    "Random Float Var Input": () => new RandomFloatVarInputNode(),
    Active: () => new ActiveOutputNode(),
    And: () => new AndNode(),
    Or: () => new OrNode(),
    Xor: () => new XorNode(),
    Not: () => new NotNode(),
    "Greater Than": () => new GreaterThanNode(),
    "Greater Than or Equal": () => new GreaterThanOrEqualNode(),
    "Less Than": () => new LessThanNode(),
    "Less Than or Equal": () => new LessThanOrEqualNode(),
    "Equal To": () => new EqualToNode(),
    "If / Else": () => new IfElseNode(),
    Add: () => new AddNode(),
    Subtract: () => new SubtractNode(),
    Multiply: () => new MultiplyNode(),
    Divide: () => new DivideNode(),
    "Parent Wood Cap": () => new ParentWoodCapNode(),
    "Clamp Max": () => new ClampMaxNode(),
    Growth: () => new GrowthNode(),
    "Delta Energy": () => new DeltaEnergyNode(),
    "Delta Water": () => new DeltaWaterNode(),
    "Delta Wood": () => new DeltaWoodNode(),
    "Set Wood": () => new SetWoodNode(),
    "Multiply Energy": () => new MultiplyEnergyNode(),
    "Multiply Water": () => new MultiplyWaterNode(),
    "Set Energy": () => new SetEnergyNode(),
    "Set Auxins": () => new SetAuxinsNode(),
    "Set trySpawn": () => new SetTrySpawnNode(),
    "Accumulate Production": () => new AccumulateProductionNode(),
    "Accumulate Env Resources": () => new AccumulateEnvResourcesNode(),
    "Accumulate Env Resources Inv": () => new AccumulateEnvResourcesInvNode(),
    "Make Bud": () => new MakeBudNode(),
    "Create Leaves": () => new CreateLeavesNode(),
    Death: () => new DeathNode(),
    "Death Parent": () => new DeathParentNode(),
    "Death Children": () => new DeathChildrenNode(),
    "Become Meristem": () => new BecomeMeristemNode(),
    "Set Lateral Angle": () => new SetLateralAngleNode(),
    "Delta Dominance": () => new DeltaDominanceNode(),
    "Set Length Var": () => new SetLengthVarNode(),
    "Turn Upwards": () => new TurnUpwardsNode(),
    "Set Was Meristem": () => new SetWasMeristemNode(),
    "Become Stem": () => new BecomeStemNode(),
    "Become Flower Stem": () => new BecomeFlowerStemNode(),
    "Become Flower Meristem": () => new BecomeFlowerMeristemNode(),
    "Spawn Meristem": () => new SpawnMeristemNode(),
    "Spawn Bud": () => new SpawnBudNode(),
    "Spawn Stem": () => new SpawnStemNode(),
    "Spawn Flower Stem": () => new SpawnFlowerStemNode(),
    "Spawn Flower Meristem": () => new SpawnFlowerMeristemNode(),
    "Spawn Flower Bud": () => new SpawnFlowerBudNode(),
    "Spawn Flower Padel": () => new SpawnFlowerPadelNode(),
    "Spawn Rhizome": () => new SpawnRhizomeNode(),
};

export function isSupportedBehaviorExportLabel(label: string): boolean {
    return Object.prototype.hasOwnProperty.call(creators, label);
}

/** Rebuild a Rete node from exported label + data (used by fromJSON). Returns null if label is not supported. */
export async function createNodeFromExport(data: { id: string; label: string; data: any }): Promise<any | null> {
    const label = data.label;
    const d = (data.data && typeof data.data === "object" ? data.data : {}) as Record<string, unknown>;
    const create = creators[label];
    if (!create) {
        console.warn("[nodeFactory] Unsupported node label (not in hud/nodes):", label, "id:", data.id);
        return null;
    }
    const node = create(d);
    if (node) applyNodeComment(node, d);
    return node;
}
