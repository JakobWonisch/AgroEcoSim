import { ClassicPreset } from 'rete';
import type { ExportedGraph } from './Conversion';
import { BooleanInputNode } from './input/BooleanInputNode';
import { ConfigurationValueInputNode } from './input/ConfigurationValueInputNode';
import { NumberInputNode } from './input/NumberInputNode';
import {
    isDuplicateConfigLabel,
    newBehaviorConfigId,
    uniqueConfigKey,
    type BehaviorConfigEntry,
    type BehaviorConfigType,
} from './behaviorConfiguration';
import type { EditorContext } from './editorContext';
import type { Species } from '../../../helpers/Species';
import { graphUpdateTrigger } from './graphUpdate';

export async function replaceConstantWithConfigInput(
    node: NumberInputNode | BooleanInputNode,
    ctx: EditorContext,
) {
    const comment = typeof (node as { comment?: string }).comment === 'string'
        ? (node as { comment?: string }).comment!.trim()
        : '';
    if (!comment) return;

    const entries = ctx.species.behaviorConfiguration.peek();
    if (isDuplicateConfigLabel(entries, comment)) return;

    const type: BehaviorConfigType = node instanceof BooleanInputNode ? 'boolean' : 'number';
    const value = type === 'boolean'
        ? (node as BooleanInputNode).switchControl.value
        : (node as NumberInputNode).valueControl.value;

    const configId = newBehaviorConfigId();
    const key = uniqueConfigKey(entries, comment);

    ctx.species.behaviorConfiguration.value = [
        ...entries,
        { id: configId, key, label: comment, type, value },
    ];

    await replaceNodeInEditor(node.id, () => new ConfigurationValueInputNode(configId, type), ctx);
}

export async function rebindConfigurationInput(
    node: ConfigurationValueInputNode,
    configId: string,
    type: BehaviorConfigType,
    ctx: EditorContext,
) {
    if (node.configId === configId && node.configType === type) {
        ctx.pushGraph();
        graphUpdateTrigger.dispatchEvent(new Event('update'));
        return;
    }

    if (node.configType === type) {
        node.configId = configId;
        node.configControl.configId = configId;
        ctx.pushGraph();
        graphUpdateTrigger.dispatchEvent(new Event('update'));
        return;
    }

    const sourceOutput = type === 'boolean' ? 'bool' : 'num';
    await replaceNodeInEditor(
        node.id,
        () => new ConfigurationValueInputNode(configId, type),
        ctx,
        sourceOutput,
    );
    graphUpdateTrigger.dispatchEvent(new Event('update'));
}

export function replaceConfigNodesInGraph(
    graph: ExportedGraph,
    configId: string,
    entry: BehaviorConfigEntry,
    comment: string,
): ExportedGraph {
    const deletedComment = comment;
    const nodes = graph.nodes.map(n => {
        if (n.label !== 'Configuration Value Input' || n.data?.configId !== configId)
            return n;
        return {
            ...n,
            label: entry.type === 'boolean' ? 'Boolean Input' : 'Number Input',
            data: entry.type === 'boolean'
                ? { bool: entry.value, comment: deletedComment }
                : { value: entry.value, comment: deletedComment },
        };
    });
    return { ...graph, nodes };
}

export async function replaceConfigInputWithConstant(
    node: ConfigurationValueInputNode,
    entry: BehaviorConfigEntry,
    comment: string,
    ctx: EditorContext,
) {
    await replaceNodeInEditor(node.id, () => {
        if (entry.type === 'boolean') {
            const n = new BooleanInputNode(Boolean(entry.value));
            (n as { comment?: string }).comment = comment;
            return n;
        }
        const n = new NumberInputNode(Number(entry.value));
        (n as { comment?: string }).comment = comment;
        return n;
    }, ctx);
}

async function replaceNodeInEditor(
    nodeId: string,
    createNode: () => ClassicPreset.Node,
    ctx: EditorContext,
    requiredSourceOutput?: string,
) {
    const position = ctx.area.nodeViews.get(nodeId)?.position ?? { x: 0, y: 0 };
    const connections = ctx.editor.getConnections().filter(
        c => c.source === nodeId || c.target === nodeId,
    );

    const newNode = createNode();
    newNode.id = nodeId;

    await ctx.editor.removeNode(nodeId);
    await ctx.editor.addNode(newNode);
    await ctx.area.translate(nodeId, position);

    for (const c of connections) {
        if (requiredSourceOutput && c.source === nodeId && c.sourceOutput !== requiredSourceOutput)
            continue;
        try {
            const sourceNode = c.source === nodeId ? newNode : ctx.editor.getNode(c.source);
            const targetNode = c.target === nodeId ? newNode : ctx.editor.getNode(c.target);
            if (!sourceNode || !targetNode) continue;
            await ctx.editor.addConnection(new ClassicPreset.Connection(
                sourceNode,
                c.sourceOutput,
                targetNode,
                c.targetInput,
            ));
        } catch {
            // ignore broken edges after socket changes
        }
    }

    ctx.pushGraph();
}

export async function deleteConfigurationEntry(
    species: Species,
    entryId: string,
    liveCtx: EditorContext | null,
) {
    const entry = species.behaviorConfiguration.peek().find(e => e.id === entryId);
    if (!entry) return;

    const comment = `Deleted Config: ${entry.label || entry.key}`;

    species.behaviorGraphs.value = species.behaviorGraphs.peek().map(ng => ({
        ...ng,
        graph: replaceConfigNodesInGraph(ng.graph, entryId, entry, comment),
    }));

    species.behaviorConfiguration.value = species.behaviorConfiguration.peek().filter(e => e.id !== entryId);

    if (liveCtx) {
        const nodes = liveCtx.editor.getNodes().filter(
            (n): n is ConfigurationValueInputNode =>
                n instanceof ConfigurationValueInputNode && n.configId === entryId,
        );
        for (const node of nodes) {
            await replaceConfigInputWithConstant(node, entry, comment, liveCtx);
        }
    }

    graphUpdateTrigger.dispatchEvent(new Event('update'));
}

export function isConfigurationInputConnected(node: ConfigurationValueInputNode, ctx: EditorContext): boolean {
    return ctx.editor.getConnections().some(
        c => c.source === node.id && (c.sourceOutput === 'num' || c.sourceOutput === 'bool'),
    );
}
