import { BaseSchemes, NodeEditor } from "rete";
import { BaseAreaPlugin } from "rete-area-plugin";

export interface ExportedGraph {
    nodes: {
        id: string;
        label: string;
        data: any;
        position: { x: number; y: number };
    }[];
    connections: {
        id: string;
        source: string;
        sourceOutput: string;
        target: string;
        targetInput: string;
    }[];
}

/** One behavior graph in a per-species ordered list (editor + wire format). */
export interface NamedGraph {
    id: string;
    name: string;
    graph: ExportedGraph;
}

export function newBehaviorGraphId(): string {
    if (typeof crypto !== "undefined" && "randomUUID" in crypto) return crypto.randomUUID();
    return `g-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 11)}`;
}

/** Boolean Input(true) wired into Active.isActive — gate defaults to active until rewired. */
export function createDefaultExportedGraph(): ExportedGraph {
    const boolId = newBehaviorGraphId();
    const activeId = newBehaviorGraphId();
    const connId = newBehaviorGraphId();
    return {
        nodes: [
            { id: boolId, label: "Boolean Input", data: { bool: true }, position: { x: 0, y: 0 } },
            { id: activeId, label: "Active", data: {}, position: { x: 220, y: 0 } },
        ],
        connections: [
            {
                id: connId,
                source: boolId,
                sourceOutput: "bool",
                target: activeId,
                targetInput: "isActive",
            },
        ],
    };
}

export function createDefaultNamedGraph(displayName: string): NamedGraph {
    return { id: newBehaviorGraphId(), name: displayName, graph: createDefaultExportedGraph() };
}

import { exportNodeComment } from "./nodeComment";

function safeClone<T>(value: T): T {
    return JSON.parse(JSON.stringify(value));
}

function exportNodeData(node: any): Record<string, unknown> {
    const out: Record<string, unknown> = {};
    if (node?.valueControl && typeof node.valueControl.value === "number")
        out.value = node.valueControl.value;
    if (node?.switchControl && typeof node.switchControl.value === "boolean")
        out.bool = node.switchControl.value;
    if (typeof node?.configId === "string" && node.configId)
        out.configId = node.configId;
    if (node?.configType === "boolean" || node?.configType === "number")
        out.configType = node.configType;
    if (node && typeof node.data === "object" && node.data !== null)
        Object.assign(out, safeClone(node.data));
    exportNodeComment(node, out);
    return out;
}

export function toJSON<Schemes extends BaseSchemes>(
    editor: NodeEditor<Schemes>,
    area: BaseAreaPlugin<Schemes, any>
): ExportedGraph {
    const nodes = editor.getNodes();
    const connections = editor.getConnections();


    return {
        nodes: nodes.map((node: any) => {
            // Get the view for this node to find its current position
            const view = area.nodeViews.get(node.id);

            return {
                id: node.id,
                label: node.label || node.constructor.name,
                data: exportNodeData(node),
                position: view ? { x: view.position.x, y: view.position.y } : { x: 0, y: 0 },
            };
        }),
        connections: connections.map((conn: any) => ({
            id: conn.id,
            source: conn.source,
            sourceOutput: conn.sourceOutput,
            target: conn.target,
            targetInput: conn.targetInput,
        })),
    };
}


type NodeFactory<S> = (data: { id: string; label: string; data: any }) => Promise<any | null>;

export async function fromJSON<S extends BaseSchemes>(
    data: ExportedGraph,
    editor: NodeEditor<S>,
    area: BaseAreaPlugin<S, any>,
    createNode: NodeFactory<S>
) {
    await editor.clear();

    const addedIds = new Set<string>();

    for (const n of data.nodes) {
        const node = await createNode(n);
        if (!node) continue;

        node.id = n.id;

        await editor.addNode(node);

        await area.translate(node.id, n.position);

        addedIds.add(n.id);
    }

    for (const c of data.connections) {
        if (!addedIds.has(c.source) || !addedIds.has(c.target))
            continue;
        try {
            await editor.addConnection({
                id: c.id,
                source: c.source,
                sourceOutput: c.sourceOutput,
                target: c.target,
                targetInput: c.targetInput,
            } as any);
        } catch {
            // ignore broken edges (e.g. socket mismatch after type changes)
        }
    }
}