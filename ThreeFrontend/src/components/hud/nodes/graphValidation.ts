export type GraphEdge = { source: string; target: string };

export function buildAdjacency(connections: GraphEdge[]) {
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

/** True if adding source → target would close a directed cycle. */
export function wouldCreateCycle(existing: GraphEdge[], source: string, target: string): boolean {
    if (source === target)
        return true;

    const { successors } = buildAdjacency(existing);
    const queue = [target];
    const visited = new Set<string>();

    while (queue.length > 0) {
        const id = queue.shift()!;
        if (id === source)
            return true;
        if (!visited.add(id))
            continue;
        for (const succ of successors.get(id) ?? [])
            queue.push(succ);
    }

    return false;
}

function kahnTopologicalSort(
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

    return order;
}

/** Mirrors backend BehaviorGraphCompiler.TryTopologicalOrder cycle detection. */
export function hasCycle(nodeIds: string[], connections: GraphEdge[]): boolean {
    const { successors, predecessors } = buildAdjacency(connections);
    const order = kahnTopologicalSort(nodeIds, successors, predecessors);
    return order.length !== nodeIds.length;
}

/** Kahn topological order; leftover nodes appended for layout when cyclic. */
export function topologicalSortForLayout(
    nodeIds: string[],
    successors: Map<string, string[]>,
    predecessors: Map<string, string[]>
): string[] {
    const order = kahnTopologicalSort(nodeIds, successors, predecessors);
    for (const id of nodeIds) {
        if (!order.includes(id))
            order.push(id);
    }
    return order;
}
