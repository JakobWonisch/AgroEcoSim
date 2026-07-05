import type { NodeEditor } from 'rete';
import type { AreaPlugin } from 'rete-area-plugin';
import type { EditorContext } from './editorContext';
import type { AreaExtra, Schemes } from './NodeTypes';
import { notifyGraphUiUpdate } from './graphUpdate';

export type CollapsibleNode = { collapsed?: boolean };

export function isNodeCollapsed(node: CollapsibleNode): boolean {
    return node.collapsed === true;
}

export function applyNodeCollapsed(node: CollapsibleNode, data: Record<string, unknown>) {
    if (data.collapsed === true)
        node.collapsed = true;
    else
        delete node.collapsed;
}

export function exportNodeCollapsed(node: CollapsibleNode, out: Record<string, unknown>) {
    if (node.collapsed === true)
        out.collapsed = true;
}

export function getConnectedPortKeys(editor: NodeEditor<Schemes>, nodeId: string) {
    const connections = editor.getConnections();
    return {
        inputs: new Set(
            connections.filter(c => c.target === nodeId).map(c => c.targetInput),
        ),
        outputs: new Set(
            connections.filter(c => c.source === nodeId).map(c => c.sourceOutput),
        ),
    };
}

export async function refreshNodeAfterCollapse(
    area: AreaPlugin<Schemes, AreaExtra>,
    editor: NodeEditor<Schemes>,
    nodeId: string,
) {
    await area.update('node', nodeId);
    const related = editor.getConnections().filter(
        c => c.source === nodeId || c.target === nodeId,
    );
    for (const c of related)
        await area.update('connection', c.id);
    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    await area.update('node', nodeId);
}

export async function setNodeCollapsed(
    ctx: EditorContext,
    node: CollapsibleNode & { id: string },
    collapsed: boolean,
) {
    if (collapsed)
        node.collapsed = true;
    else
        delete node.collapsed;
    await refreshNodeAfterCollapse(ctx.area, ctx.editor, node.id);
    notifyGraphUiUpdate();
    ctx.pushGraph();
}

export async function setAllNodesCollapsed(ctx: EditorContext, collapsed: boolean) {
    for (const node of ctx.editor.getNodes()) {
        if (collapsed)
            (node as CollapsibleNode).collapsed = true;
        else
            delete (node as CollapsibleNode).collapsed;
    }
    for (const node of ctx.editor.getNodes())
        await refreshNodeAfterCollapse(ctx.area, ctx.editor, node.id);
    notifyGraphUiUpdate();
    ctx.pushGraph();
}
