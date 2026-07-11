import { AreaPlugin } from "rete-area-plugin";
import { NodeEditor } from "rete";
import { AreaExtra, Schemes } from "./NodeTypes";
import { buildAdjacency, topologicalSortForLayout } from "./graphValidation";

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

/** As far right as possible within mainSet, but not left of active predecessors + 1. */
function computeMainColumns(
    topoOrder: string[],
    successors: Map<string, string[]>,
    predecessors: Map<string, string[]>,
    mainSet: Set<string>,
    activeColumns: Map<string, number>
): Map<string, number> {
    const lower = new Map<string, number>();
    for (const id of topoOrder) {
        if (!mainSet.has(id))
            continue;
        let bound = 0;
        for (const pred of predecessors.get(id) ?? []) {
            if (activeColumns.has(pred))
                bound = Math.max(bound, activeColumns.get(pred)! + 1);
            else if (mainSet.has(pred))
                bound = Math.max(bound, (lower.get(pred) ?? 0) + 1);
        }
        lower.set(id, bound);
    }

    const column = new Map<string, number>();
    for (const id of [...topoOrder].reverse()) {
        if (!mainSet.has(id))
            continue;
        const succs = (successors.get(id) ?? []).filter(s => mainSet.has(s));
        let col = succs.length === 0 ? 0 : Math.min(...succs.map(s => column.get(s)!)) - 1;
        col = Math.max(col, lower.get(id)!);
        column.set(id, col);
    }

    const members = topoOrder.filter(id => mainSet.has(id));
    const minCol = Math.min(...members.map(id => column.get(id)!));
    if (minCol < 0) {
        for (const id of members)
            column.set(id, column.get(id)! - minCol);
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

function placeDualRegion(
    activeSet: Set<string>,
    mainSet: Set<string>,
    activeColumns: Map<string, number>,
    mainColumns: Map<string, number>,
    topoOrder: string[],
    sizes: Map<string, Size>,
    rowMargin: number,
    columnMargin: number,
    activeMargin: number
): Map<string, Position> {
    const topoIndex = new Map(topoOrder.map((id, i) => [id, i]));
    const sortedCols = [...new Set([
        ...[...activeSet].map(id => activeColumns.get(id) ?? 0),
        ...[...mainSet].map(id => mainColumns.get(id) ?? 0),
    ])].sort((a, b) => a - b);

    const colWidth = new Map<number, number>();
    for (const col of sortedCols) {
        let w = 0;
        for (const id of activeSet) {
            if ((activeColumns.get(id) ?? 0) === col)
                w = Math.max(w, sizes.get(id)!.width);
        }
        for (const id of mainSet) {
            if ((mainColumns.get(id) ?? 0) === col)
                w = Math.max(w, sizes.get(id)!.width);
        }
        colWidth.set(col, w);
    }

    const colX = new Map<number, number>();
    let x = rowMargin;
    for (const col of sortedCols) {
        colX.set(col, x);
        x += colWidth.get(col)! + columnMargin;
    }

    const positions = new Map<string, Position>();

    const stackBand = (memberSet: Set<string>, columns: Map<string, number>, originY: number): number => {
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

        let regionBottom = originY;
        for (const col of sortedCols) {
            const ids = byColumn.get(col);
            if (!ids)
                continue;
            const colXPos = colX.get(col)!;
            let y = originY;
            for (const id of ids) {
                const { height } = sizes.get(id)!;
                positions.set(id, { x: colXPos, y });
                regionBottom = Math.max(regionBottom, y + height);
                y += height + rowMargin;
            }
        }
        return regionBottom - originY;
    };

    const activeHeight = stackBand(activeSet, activeColumns, rowMargin);
    if (mainSet.size > 0)
        stackBand(mainSet, mainColumns, rowMargin + activeHeight + activeMargin);

    return positions;
}

function layoutSingleRegion(
    nodeIds: string[],
    topoOrder: string[],
    successors: Map<string, string[]>,
    predecessors: Map<string, string[]>,
    sizes: Map<string, Size>,
    rowMargin: number,
    columnMargin: number
): Map<string, Position> {
    const memberSet = new Set(nodeIds);
    const columns = computeMainColumns(topoOrder, successors, predecessors, memberSet, new Map());
    return placeDualRegion(
        new Set(), memberSet, new Map(), columns,
        topoOrder, sizes, rowMargin, columnMargin, 0
    );
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
    const topoOrder = topologicalSortForLayout(nodeIds, successors, predecessors);
    const sizes = new Map(nodeIds.map(id => [id, measureNode(area, id)]));

    const activeId = findActiveNodeId(nodes);
    if (!activeId)
        return layoutSingleRegion(nodeIds, topoOrder, successors, predecessors, sizes, rowMargin, columnMargin);

    const activeSet = computeActiveSubtree(activeId, connectionsFull);
    const mainSet = new Set(nodeIds.filter(id => !activeSet.has(id)));
    const activeColumns = assignColumnsEarly(topoOrder, predecessors, activeSet);
    const mainColumns = computeMainColumns(topoOrder, successors, predecessors, mainSet, activeColumns);

    return placeDualRegion(
        activeSet, mainSet, activeColumns, mainColumns,
        topoOrder, sizes, rowMargin, columnMargin, activeMargin
    );
}

export async function applyAutoLayout(
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>
): Promise<void> {
    const positions = computeAutoLayoutPositions(editor, area);
    for (const [id, pos] of positions)
        await area.translate(id, pos);
}
