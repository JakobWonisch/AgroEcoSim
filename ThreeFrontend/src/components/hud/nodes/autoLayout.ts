import { AreaPlugin } from "rete-area-plugin";
import { NodeEditor } from "rete";
import { AreaExtra, Schemes } from "./NodeTypes";

/** Vertical gap in pixels between nodes stacked in the same column. */
export const AUTO_LAYOUT_ROW_MARGIN = 48;

/** Horizontal gap in pixels between columns. */
export const AUTO_LAYOUT_COLUMN_MARGIN = 120;

/** Vertical gap in pixels between the Active gate subgraph and the main graph below. */
export const AUTO_LAYOUT_ACTIVE_MARGIN = 96;

const DEFAULT_NODE_WIDTH = 200;
const DEFAULT_NODE_HEIGHT = 80;

const ACTIVE_NODE_LABEL = "Active";
const ACTIVE_INPUT_KEY = "isActive";

type Position = { x: number; y: number };
type Size = { width: number; height: number };
type Connection = { source: string; target: string; targetInput: string };

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

/** The single Active node id, if the graph has exactly one. */
function findActiveNodeId(nodes: { id: string; label?: string }[]): string | null {
    const actives = nodes.filter(n => n.label === ACTIVE_NODE_LABEL);
    return actives.length === 1 ? actives[0].id : null;
}

/**
 * Active gate subgraph: Active plus all upstream producers of `isActive`,
 * recursively including every input of each visited node (matches backend compiler).
 */
function computeActiveSubtree(activeId: string, connections: Connection[]): Set<string> {
    const subtree = new Set<string>([activeId]);

    const markUpstream = (nodeId: string, visited: Set<string>) => {
        if (!visited.add(nodeId))
            return;
        subtree.add(nodeId);
        for (const c of connections) {
            if (c.target === nodeId)
                markUpstream(c.source, visited);
        }
    };

    for (const c of connections) {
        if (c.target === activeId && c.targetInput === ACTIVE_INPUT_KEY)
            markUpstream(c.source, new Set());
    }

    return subtree;
}

/** As far left as possible: predecessors in the same member set sit in earlier columns. */
function assignColumnsEarly(
    topoOrder: string[],
    predecessors: Map<string, string[]>,
    memberSet: Set<string>
): Map<string, number> {
    const column = new Map<string, number>();

    for (const id of topoOrder) {
        if (!memberSet.has(id))
            continue;
        const preds = (predecessors.get(id) ?? []).filter(p => memberSet.has(p));
        column.set(id, preds.length === 0 ? 0 : Math.max(...preds.map(p => column.get(p) ?? 0)) + 1);
    }

    return column;
}

/** As far right as possible: successors in the same member set sit in later columns. */
function assignColumnsLate(
    topoOrder: string[],
    successors: Map<string, string[]>,
    memberSet: Set<string>
): Map<string, number> {
    const column = new Map<string, number>();
    const members = topoOrder.filter(id => memberSet.has(id));

    for (const id of [...topoOrder].reverse()) {
        if (!memberSet.has(id))
            continue;
        const succs = (successors.get(id) ?? []).filter(s => memberSet.has(s));
        column.set(id, succs.length === 0 ? 0 : Math.min(...succs.map(s => column.get(s) ?? 0)) - 1);
    }

    const minCol = Math.min(...members.map(id => column.get(id) ?? 0));
    if (minCol !== 0) {
        for (const id of members)
            column.set(id, (column.get(id) ?? 0) - minCol);
    }

    return column;
}

function measureNode(
    area: AreaPlugin<Schemes, AreaExtra>,
    nodeId: string
): Size {
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

function placeSubgraph(
    memberSet: Set<string>,
    columns: Map<string, number>,
    topoOrder: string[],
    sizes: Map<string, Size>,
    originX: number,
    originY: number,
    rowMargin: number,
    columnMargin: number
): { positions: Map<string, Position>; height: number } {
    const topoIndex = new Map(topoOrder.map((id, i) => [id, i]));
    const byColumn = new Map<number, string[]>();

    for (const id of memberSet) {
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

    const positions = new Map<string, Position>();
    let regionBottom = originY;
    let x = originX;

    for (const col of [...byColumn.keys()].sort((a, b) => a - b)) {
        const ids = byColumn.get(col)!;
        const colWidth = Math.max(...ids.map(id => sizes.get(id)!.width));
        let y = originY;

        for (const id of ids) {
            const { height } = sizes.get(id)!;
            positions.set(id, { x, y });
            regionBottom = Math.max(regionBottom, y + height);
            y += height + rowMargin;
        }

        x += colWidth + columnMargin;
    }

    return { positions, height: regionBottom - originY };
}

function layoutSingleRegion(
    nodeIds: string[],
    topoOrder: string[],
    successors: Map<string, string[]>,
    sizes: Map<string, Size>,
    rowMargin: number,
    columnMargin: number
): Map<string, Position> {
    const memberSet = new Set(nodeIds);
    const columns = assignColumnsLate(topoOrder, successors, memberSet);
    return placeSubgraph(memberSet, columns, topoOrder, sizes, rowMargin, rowMargin, rowMargin, columnMargin).positions;
}

export function computeAutoLayoutPositions(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>,
    rowMargin: number = AUTO_LAYOUT_ROW_MARGIN,
    columnMargin: number = AUTO_LAYOUT_COLUMN_MARGIN,
    activeMargin: number = AUTO_LAYOUT_ACTIVE_MARGIN
): Map<string, Position> {
    const nodes = editor.getNodes();
    if (nodes.length === 0)
        return new Map();

    const nodeIds = nodes.map(n => n.id);
    const connectionsFull: Connection[] = editor.getConnections().map(c => ({
        source: c.source,
        target: c.target,
        targetInput: c.targetInput,
    }));
    const { successors, predecessors } = buildAdjacency(connectionsFull);
    const topoOrder = topologicalSort(nodeIds, successors, predecessors);
    const sizes = new Map(nodeIds.map(id => [id, measureNode(area, id)]));

    const activeId = findActiveNodeId(nodes);
    if (!activeId)
        return layoutSingleRegion(nodeIds, topoOrder, successors, sizes, rowMargin, columnMargin);

    const activeSet = computeActiveSubtree(activeId, connectionsFull);
    const mainSet = new Set(nodeIds.filter(id => !activeSet.has(id)));
    const positions = new Map<string, Position>();

    const activeColumns = assignColumnsEarly(topoOrder, predecessors, activeSet);
    const activeLayout = placeSubgraph(
        activeSet, activeColumns, topoOrder, sizes,
        rowMargin, rowMargin, rowMargin, columnMargin
    );
    for (const [id, pos] of activeLayout.positions)
        positions.set(id, pos);

    if (mainSet.size > 0) {
        const mainStartY = rowMargin + activeLayout.height + activeMargin;
        const mainColumns = assignColumnsLate(topoOrder, successors, mainSet);
        const mainLayout = placeSubgraph(
            mainSet, mainColumns, topoOrder, sizes,
            rowMargin, mainStartY, rowMargin, columnMargin
        );
        for (const [id, pos] of mainLayout.positions)
            positions.set(id, pos);
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
