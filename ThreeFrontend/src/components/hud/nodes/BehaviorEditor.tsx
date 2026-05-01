import { h, render } from 'preact';
import { useMemo } from 'preact/hooks';
import {
    ClassicPreset,
    NodeEditor
} from 'rete';
import { AreaExtensions, AreaPlugin } from 'rete-area-plugin';
import { ConnectionPlugin, Presets as ConnectionPresets } from 'rete-connection-plugin';
import { ContextMenuPlugin, Presets as ContextMenuPresets } from 'rete-context-menu-plugin';
import { Presets, ReactPlugin, useRete } from 'rete-react-plugin';
import { CustomInputComponent, CustomSocketComponent, SwitchControl, SwitchControlComponent } from './Controls';
import { AgentTypeNode } from './input/AgentTypeNode';
import { OrganSensorsNode } from './input/OrganSensorsNode';
import { BooleanInputNode } from './input/BooleanInputNode';
import { NumberInputNode } from './input/NumberInputNode';
import { AreaExtra, Schemes } from './NodeTypes';
import { ActiveOutputNode } from './output/ActiveOutputNode';
import { BooleanOutputNode } from './output/BooleanOutputNode';
import { NumberOutputNode } from './output/NumberOutputNode';
import { GrowthNode } from './output/GrowthNode';
import { AndNode } from './util/boolean/AndNode';
import { NotNode } from './util/boolean/NotNode';
import { OrNode } from './util/boolean/OrNode';
import { XorNode } from './util/boolean/XorNode';
import { EqualToNode } from './util/logic/EqualToNode';
import { GreaterThanNode } from './util/logic/GreaterThanNode';
import { IfElseNode } from './util/logic/IfElseNode';
import { LessThanNode } from './util/logic/LessThanNode';
import { AddNode } from './util/numeric/AddNode';
import { DivideNode } from './util/numeric/DivideNode';
import { MultiplyNode } from './util/numeric/MultiplyNode';
import { SubtractNode } from './util/numeric/SubtractNode';
import type { Species } from '../../../helpers/Species';
import appstate from '../../../appstate';
import { fromJSON, toJSON } from './Conversion';
import { createNodeFromExport } from './nodeFactory';

export const DEBUG_SHOW_VALUES = true;

function pushSpeciesGraph(species: Species, editor: NodeEditor<Schemes>, area: AreaPlugin<Schemes, AreaExtra>) {
    species.behaviorGraph.value = toJSON(editor, area);
}

async function processGraph(editor: NodeEditor<Schemes>, area: AreaPlugin<Schemes, AreaExtra>) {
    const cache = new Map<string, any>();

    async function evaluateNode(nodeId: string): Promise<any> {
        if (cache.has(nodeId)) return cache.get(nodeId);

        const node = editor.getNode(nodeId);
        const inputsData: Record<string, any[]> = {};

        const cons = editor.getConnections().filter(c => c.target === nodeId);
        for (const c of cons) {
            const outData = await evaluateNode(c.source);
            if (!inputsData[c.targetInput]) inputsData[c.targetInput] = [];
            if (outData && outData[c.sourceOutput] !== undefined) {
                inputsData[c.targetInput].push(outData[c.sourceOutput]);
            }
        }

        const data = ('data' in node && typeof (node as any).data === 'function')
            ? (node as any).data(inputsData)
            : {};

        // Debug labeling
        let updated = false;
        if (node.inputs) {
            for (const [key, input] of Object.entries(node.inputs)) {
                if (!input) continue;
                const baseLabel = input.label.split(' [')[0];
                let newLabel = baseLabel;

                if (DEBUG_SHOW_VALUES && inputsData[key] && inputsData[key].length > 0) {
                    const val = inputsData[key][0];
                    const valStr = typeof val === 'boolean' ? (val ? 'True' : 'False') : (typeof val === 'number' ? Number(val).toFixed(2) : String(val));
                    newLabel = `${baseLabel} [${valStr}]`;
                }

                if (input.label !== newLabel) {
                    input.label = newLabel;
                    updated = true;
                }
            }
        }

        cache.set(nodeId, data);
        if (updated) {
            area.update('node', nodeId);
        }
        return data;
    }

    // Evaluate all nodes
    for (const node of editor.getNodes()) {
        await evaluateNode(node.id);
    }
}

export async function createEditor(container: HTMLElement, species: Species) {
    const editor = new NodeEditor<Schemes>();
    const area = new AreaPlugin<Schemes, AreaExtra>(container);
    const connection = new ConnectionPlugin<Schemes, AreaExtra>();

    // Custom createRoot wrapper for Preact
    const createRoot = (container: HTMLElement) => ({
        render: (element: any) => render(element as any, container),
        unmount: () => render(null, container)
    });

    const renderPlugin = new ReactPlugin<Schemes, AreaExtra>({ createRoot });

    renderPlugin.addPreset(Presets.classic.setup({
        customize: {
            control(data) {
                if (data.payload instanceof SwitchControl) {
                    return SwitchControlComponent as any;
                }
                if (data.payload instanceof ClassicPreset.InputControl) {
                    return CustomInputComponent as any;
                }
                return null;
            },
            socket(data) {
                return CustomSocketComponent as any;
            }
        }
    }));

    renderPlugin.addPreset(Presets.contextMenu.setup({ delay: 0 }));

    const contextMenu = new ContextMenuPlugin<Schemes>({
        items: ContextMenuPresets.classic.setup([
            ['input', [
                ['Number', () => new NumberInputNode(0)],
                ['Boolean', () => new BooleanInputNode(false)],
                ['Agent Type', () => new AgentTypeNode()],
                ['Organ Sensors', () => new OrganSensorsNode()]
            ]],
            ['output', [
                ['Active', () => new ActiveOutputNode()],
                ['Number', () => new NumberOutputNode()],
                ['Boolean', () => new BooleanOutputNode()],
                ['Growth', () => new GrowthNode()]
            ]],
            ['boolean', [
                ['And', () => new AndNode()],
                ['Or', () => new OrNode()],
                ['Xor', () => new XorNode()],
                ['Not', () => new NotNode()]
            ]],
            ['numeric', [
                ['Add', () => new AddNode()],
                ['Subtract', () => new SubtractNode()],
                ['Multiply', () => new MultiplyNode()],
                ['Divide', () => new DivideNode()]
            ]],
            ['logic', [
                ['Greater Than (or Equal)', () => new GreaterThanNode()],
                ['Less Than (or Equal)', () => new LessThanNode()],
                ['Equal To', () => new EqualToNode()],
                ['If / Else', () => new IfElseNode()]
            ]]
        ])
    });

    connection.addPreset(ConnectionPresets.classic.setup());

    let lastPointerEvent: MouseEvent | undefined;
    let pendingDropPosition: { x: number, y: number } | null = null;
    let pendingDropConnection: { nodeId: string, side: 'input' | 'output', key: string } | null = null;

    area.addPipe(context => {
        const c = context as any;
        if (['pointermove', 'pointerup'].includes(c.type)) {
            if (c.data && c.data.event) {
                lastPointerEvent = c.data.event;
            }
        }
        if (c.type === 'pointerdown') {
            pendingDropPosition = null;
            pendingDropConnection = null;
        }
        return context;
    });

    connection.addPipe(context => {
        const c = context as any;
        if (c.type === 'connectiondrop') {
            const ev = c.data.event || lastPointerEvent;
            if (ev) {
                // Record the exact projected SVG coordinates
                pendingDropPosition = { ...area.area.pointer };
                pendingDropConnection = c.data.initial;

                setTimeout(() => {
                    area.emit({ type: 'contextmenu', data: { event: ev, context: 'root' } } as any);
                }, 10);
            }
        }
        return context;
    });

    let recentlyRemovedConnection: any = null;
    let removeTimeout: any = null;

    editor.addPipe(context => {
        const c = context as any;
        if (c.type === 'connectionremove') {
            recentlyRemovedConnection = c.data;
            clearTimeout(removeTimeout);
            removeTimeout = setTimeout(() => {
                recentlyRemovedConnection = null;
            }, 50);
        }

        if (context.type === 'connectioncreate') {
            const { source, target, sourceOutput, targetInput } = context.data;
            const sourceNode = editor.getNode(source);
            const targetNode = editor.getNode(target);

            // Get the socket definitions to check compatibility
            const outSocket = sourceNode?.outputs[sourceOutput]?.socket;
            const inSocket = targetNode?.inputs[targetInput]?.socket;

            if (outSocket && inSocket && outSocket.name !== inSocket.name) {
                // Prevent the connection if sockets mismatch
                if (recentlyRemovedConnection && recentlyRemovedConnection.target === target && recentlyRemovedConnection.targetInput === targetInput) {
                    const toRestore = recentlyRemovedConnection;
                    setTimeout(() => {
                        editor.addConnection(toRestore).catch(() => { });
                    }, 10);
                }
                return;
            }
        }

        if (c.type === 'nodecreated') {
            if (pendingDropPosition) {
                const pos = { ...pendingDropPosition };
                pendingDropPosition = null;
                setTimeout(() => area.translate(c.data.id, pos), 0);
            }

            if (pendingDropConnection) {
                const src = pendingDropConnection;
                pendingDropConnection = null;

                const newNode = c.data;
                setTimeout(() => {
                    try {
                        if (src.side === 'output') {
                            const inputs = Object.entries(newNode.inputs);
                            if (inputs.length > 0) {
                                editor.addConnection(new ClassicPreset.Connection(
                                    editor.getNode(src.nodeId), src.key,
                                    newNode, inputs[0][0]
                                )).catch(() => { });
                            }
                        } else if (src.side === 'input') {
                            const outputs = Object.entries(newNode.outputs);
                            if (outputs.length > 0) {
                                editor.addConnection(new ClassicPreset.Connection(
                                    newNode, outputs[0][0],
                                    editor.getNode(src.nodeId), src.key
                                )).catch(() => { });
                            }
                        }
                    } catch (e) { }
                }, 10);
            }
        }

        if (['connectioncreated', 'connectionremoved', 'nodecreated', 'noderemoved'].includes(context.type)) {
            setTimeout(() => {
                processGraph(editor, area);
                pushSpeciesGraph(species, editor, area);
            }, 0);
        }

        return context;
    });

    import('./Controls').then(m => {
        m.graphUpdateTrigger.addEventListener('update', () => {
            setTimeout(() => {
                processGraph(editor, area);
                pushSpeciesGraph(species, editor, area);
            }, 0);
        });
    });

    editor.use(area);
    area.use(connection);
    area.use(renderPlugin);
    area.use(contextMenu);

    AreaExtensions.simpleNodesOrder(area);

    const initial = species.behaviorGraph.peek();
    if (initial?.nodes?.length > 0)
        await fromJSON(initial, editor, area, createNodeFromExport);

    const speciesName = species.name.peek();
    appstate.registerBehaviorGraphGetter(speciesName, () => toJSON(editor, area));
    pushSpeciesGraph(species, editor, area);

    setTimeout(() => {
        const nodes = editor.getNodes();
        if (nodes.length > 0)
            AreaExtensions.zoomAt(area, nodes);
        processGraph(editor, area);
        pushSpeciesGraph(species, editor, area);
    }, 10);

    const handleKeyDown = (e: KeyboardEvent) => {
        if (e.key === 'Escape') {
            area.emit({ type: 'pointerdown', data: { event: new PointerEvent('pointerdown') } } as any);
        }
    };
    window.addEventListener('keydown', handleKeyDown);

    return {
        destroy: () => {
            appstate.unregisterBehaviorGraphGetter(speciesName);
            window.removeEventListener('keydown', handleKeyDown);
            area.destroy();
        }
    };
}

export default function BehaviorEditor({ species }: { species: Species }) {
    const factory = useMemo(
        () => (container: HTMLElement) => createEditor(container, species),
        [species.name.value, species]
    );
    const [ref] = useRete(factory);

    return (
        <div style={{ width: '100%', height: '100%', background: 'rgba(0,0,0,0.1)' }}>
            <div ref={ref} style={{ width: '100%', height: '100%' }} />
        </div>
    );
}