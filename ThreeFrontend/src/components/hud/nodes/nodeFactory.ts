import { NumberInputNode } from "./input/NumberInputNode";
import { BooleanInputNode } from "./input/BooleanInputNode";
import { AgentTypeNode } from "./input/AgentTypeNode";
import { ActiveOutputNode } from "./output/ActiveOutputNode";
import { BooleanOutputNode } from "./output/BooleanOutputNode";
import { NumberOutputNode } from "./output/NumberOutputNode";
import { AndNode } from "./util/boolean/AndNode";
import { NotNode } from "./util/boolean/NotNode";
import { OrNode } from "./util/boolean/OrNode";
import { XorNode } from "./util/boolean/XorNode";
import { EqualToNode } from "./util/logic/EqualToNode";
import { GreaterThanNode } from "./util/logic/GreaterThanNode";
import { IfElseNode } from "./util/logic/IfElseNode";
import { LessThanNode } from "./util/logic/LessThanNode";
import { AddNode } from "./util/numeric/AddNode";
import { DivideNode } from "./util/numeric/DivideNode";
import { MultiplyNode } from "./util/numeric/MultiplyNode";
import { SubtractNode } from "./util/numeric/SubtractNode";

/** Labels that match `super('…')` on node classes under hud/nodes (canonical export labels). */
export const canonicalBehaviorNodeLabels = [
    "Number Input",
    "Boolean Input",
    "Agent Type",
    "Active",
    "Boolean Output",
    "Number Output",
    "And",
    "Or",
    "Xor",
    "Not",
    "Greater Than (or Equal)",
    "Less Than (or Equal)",
    "Equal To",
    "If / Else",
    "Add",
    "Subtract",
    "Multiply",
    "Divide",
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
    "Agent Type": () => new AgentTypeNode(),
    Active: () => new ActiveOutputNode(),
    "Boolean Output": () => new BooleanOutputNode(),
    "Number Output": () => new NumberOutputNode(),
    And: () => new AndNode(),
    Or: () => new OrNode(),
    Xor: () => new XorNode(),
    Not: () => new NotNode(),
    "Greater Than (or Equal)": () => new GreaterThanNode(),
    "Less Than (or Equal)": () => new LessThanNode(),
    "Equal To": () => new EqualToNode(),
    "If / Else": () => new IfElseNode(),
    Add: () => new AddNode(),
    Subtract: () => new SubtractNode(),
    Multiply: () => new MultiplyNode(),
    Divide: () => new DivideNode(),
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
    return create(d);
}
