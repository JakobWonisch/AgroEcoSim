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
                data: JSON.parse(JSON.stringify(node.data || {})), // Ensure it's serializable
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


type NodeFactory<S> = (data: { id: string; label: string; data: any }) => Promise<any>;

export async function fromJSON<S extends BaseSchemes>(
    data: ExportedGraph,
    editor: NodeEditor<S>,
    area: BaseAreaPlugin<S, any>,
    createNode: NodeFactory<S>
) {
    await editor.clear();

    const nodesMap = new Map();

    for (const n of data.nodes) {
        const node = await createNode(n);
        node.id = n.id;

        await editor.addNode(node);

        await area.translate(node.id, n.position);

        nodesMap.set(n.id, node);
    }

    for (const c of data.connections) {
        await editor.addConnection({
            id: c.id,
            source: c.source,
            sourceOutput: c.sourceOutput,
            target: c.target,
            targetInput: c.targetInput,
        } as any);
    }
}