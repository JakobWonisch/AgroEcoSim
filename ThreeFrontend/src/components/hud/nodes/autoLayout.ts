import { AreaPlugin } from "rete-area-plugin";
import { NodeEditor } from "rete";
import { AreaExtra, Schemes } from "./NodeTypes";

/** Vertical gap in pixels between nodes stacked in the same column. */
export const AUTO_LAYOUT_ROW_MARGIN = 48;

/** Horizontal gap in pixels between columns. */
export const AUTO_LAYOUT_COLUMN_MARGIN = 120;

const DEFAULT_NODE_WIDTH = 200;
const DEFAULT_NODE_HEIGHT = 80;

type Position = { x: number; y: number };

function buildAdjacency(connections: { source: string; target: string }[]) {
    const successors = new Map<string, string[]>();
    const predecessors = new Map<string, string[]>();

    const touch = (map: Map<string, string[]>, from: string, to: string) => {
        let list = map.get(from);
        if (!list) {
            list = [];
            map.set(from, list);
        }
        list.push(to);
    };

    for (const c of connections) {
        touch(successors, c.source, c.target);
        touch(predecessors, c.target, c.source);
    }

    return { successors, predecessors };
}

/** Kahn topological order; graphs are assumed acyclic. */
function topologicalSort(
    nodeIds: string[],
    successors: Map<string, string[]>,
    predecessors: Map<string, string[]>
): string[] {
    const inDegree = new Map<string, number>();
    for (const id of nodeIds)
        inDegree.set(id, (predecessors.get(id) ?? []).length);

    const queue = nodeIds.filter(id => inDegree.get(id) === 0);
    const order: string[] = [];

    while (queue.length > 0) {
        const id = queue.shift()!;
        order.push(id);
        for (const succ of successors.get(id) ?? []) {
            const next = (inDegree.get(succ) ?? 0) - 1;
            inDegree.set(succ, next);
            if (next === 0)
                queue.push(succ);
        }
    }

    for (const id of nodeIds)
        if (!order.includes(id))
            order.push(id);

    return order;
}

/**
 * Assign columns left-to-right so each node is as far right as possible while
 * keeping every edge source strictly to the left of its target.
 */
function assignColumns(topoOrder: string[], successors: Map<string, string[]>): Map<string, number> {
    const column = new Map<string, number>();

    for (const id of [...topoOrder].reverse()) {
        const succs = successors.get(id) ?? [];
        if (succs.length === 0)
            column.set(id, 0);
        else
            column.set(id, Math.min(...succs.map(s => column.get(s) ?? 0)) - 1);
    }

    const minCol = Math.min(...topoOrder.map(id => column.get(id) ?? 0));
    if (minCol !== 0) {
        for (const id of topoOrder)
            column.set(id, (column.get(id) ?? 0) - minCol);
    }

    return column;
}

function measureNode(
    area: AreaPlugin<Schemes, AreaExtra>,
    nodeId: string
): { width: number; height: number } {
    const view = area.nodeViews.get(nodeId);
    const el = view?.element;
    if (el) {
        const w = el.offsetWidth;
        const h = el.offsetHeight;
        if (w > 0 && h > 0)
            return { width: w, height: h };
    }
    return { width: DEFAULT_NODE_WIDTH, height: DEFAULT_NODE_HEIGHT };
}

export function computeAutoLayoutPositions(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>,
    rowMargin: number = AUTO_LAYOUT_ROW_MARGIN,
    columnMargin: number = AUTO_LAYOUT_COLUMN_MARGIN
): Map<string, Position> {
    const nodes = editor.getNodes();
    if (nodes.length === 0)
        return new Map();

    const nodeIds = nodes.map(n => n.id);
    const connections = editor.getConnections().map(c => ({ source: c.source, target: c.target }));
    const { successors, predecessors } = buildAdjacency(connections);
    const topoOrder = topologicalSort(nodeIds, successors, predecessors);
    const topoIndex = new Map(topoOrder.map((id, i) => [id, i]));
    const columns = assignColumns(topoOrder, successors);

    const sizes = new Map(nodeIds.map(id => [id, measureNode(area, id)]));

    const byColumn = new Map<number, string[]>();
    for (const id of nodeIds) {
        const col = columns.get(id) ?? 0;
        let list = byColumn.get(col);
        if (!list) {
            list = [];
            byColumn.set(col, list);
        }
        list.push(id);
    }

    for (const ids of byColumn.values())
        ids.sort((a, b) => (topoIndex.get(a) ?? 0) - (topoIndex.get(b) ?? 0));

    const sortedColumns = [...byColumn.keys()].sort((a, b) => a - b);
    const positions = new Map<string, Position>();

    let x = rowMargin;
    for (const col of sortedColumns) {
        const ids = byColumn.get(col)!;
        const colWidth = Math.max(...ids.map(id => sizes.get(id)!.width));
        let y = rowMargin;

        for (const id of ids) {
            const { height } = sizes.get(id)!;
            positions.set(id, { x, y });
            y += height + rowMargin;
        }

        x += colWidth + columnMargin;
    }

    return positions;
}

export async function applyAutoLayout(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>
): Promise<void> {
    const positions = computeAutoLayoutPositions(editor, area);
    for (const [id, pos] of positions)
        await area.translate(id, pos);
}
