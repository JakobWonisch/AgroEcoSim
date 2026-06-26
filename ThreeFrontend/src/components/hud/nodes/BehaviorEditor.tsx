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
import { AgentTypeInputNode } from './input/AgentTypeInputNode';
import { PhaseInputNode } from './input/PhaseInputNode';
import { AgentStateInputNode } from './input/AgentStateInputNode';
import { ParentInputNode } from './input/ParentInputNode';
import { IrradianceInputNode } from './input/IrradianceInputNode';
import { RandomChanceInputNode } from './input/RandomChanceInputNode';
import { BooleanInputNode } from './input/BooleanInputNode';
import { NumberInputNode } from './input/NumberInputNode';
import { AreaExtra, Schemes } from './NodeTypes';
import { ActiveOutputNode } from './output/ActiveOutputNode';
import { BooleanOutputNode } from './output/BooleanOutputNode';
import { NumberOutputNode } from './output/NumberOutputNode';
import { GrowthNode } from './output/GrowthNode';
import { DeltaEnergyNode } from './output/DeltaEnergyNode';
import { DeltaWaterNode } from './output/DeltaWaterNode';
import { DeltaWoodNode } from './output/DeltaWoodNode';
import { SetWoodNode } from './output/SetWoodNode';
import { MultiplyEnergyNode } from './output/MultiplyEnergyNode';
import { MultiplyWaterNode } from './output/MultiplyWaterNode';
import { SetEnergyNode } from './output/SetEnergyNode';
import { SetAuxinsNode } from './output/SetAuxinsNode';
import { SetTrySpawnNode } from './output/SetTrySpawnNode';
import { AccumulateProductionNode } from './output/AccumulateProductionNode';
import { MakeBudNode } from './output/MakeBudNode';
import { CreateLeavesNode } from './output/CreateLeavesNode';
import { DeathNode } from './output/DeathNode';
import { DeathParentNode } from './output/DeathParentNode';
import { DeathChildrenNode } from './output/DeathChildrenNode';
import { BecomeMeristemNode } from './output/BecomeMeristemNode';
import { BecomeStemNode } from './output/BecomeStemNode';
import { BecomeFlowerStemNode } from './output/BecomeFlowerStemNode';
import { BecomeFlowerMeristemNode } from './output/BecomeFlowerMeristemNode';
import {
    SpawnMeristemNode,
    SpawnBudNode,
    SpawnStemNode,
    SpawnFlowerStemNode,
    SpawnFlowerMeristemNode,
    SpawnFlowerBudNode,
    SpawnFlowerPadelNode,
    SpawnRhizomeNode,
} from './output/spawn/SpawnNodes';
import { ParentWoodCapNode } from './util/numeric/ParentWoodCapNode';
import { ClampMaxNode } from './util/numeric/ClampMaxNode';
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
import type { NamedGraph } from './Conversion';
import { fromJSON, toJSON } from './Conversion';
import { createNodeFromExport } from './nodeFactory';
import { applyAutoLayout } from './autoLayout';

function pushSpeciesGraph(species: Species, namedGraph: NamedGraph, editor: NodeEditor<Schemes>, area: AreaPlugin<Schemes, AreaExtra>) {
    // Avoid overwriting node positions with 0/0 snapshots before views are ready.
    const nodes = editor.getNodes();
    if (nodes.some((n: any) => !area.nodeViews.get(n.id)))
        return;

    const snapshot = toJSON(editor, area);
    species.behaviorGraphs.value = species.behaviorGraphs.peek().map(g =>
        g.id === namedGraph.id ? { ...g, graph: snapshot } : g);
}

export async function createEditor(container: HTMLElement, species: Species, namedGraph: NamedGraph) {
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
                ['Agent Type Input', () => new AgentTypeInputNode()],
                ['Phase Input', () => new PhaseInputNode()],
                ['Agent State Input', () => new AgentStateInputNode()],
                ['Parent Input', () => new ParentInputNode()],
                ['Irradiance Input', () => new IrradianceInputNode()],
                ['Random Chance Input', () => new RandomChanceInputNode()],
            ]],
            ['output', [
                ['Number', () => new NumberOutputNode()],
                ['Boolean', () => new BooleanOutputNode()],
                ['Growth', () => new GrowthNode()],
                ['Delta Energy', () => new DeltaEnergyNode()],
                ['Delta Water', () => new DeltaWaterNode()],
                ['Delta Wood', () => new DeltaWoodNode()],
                ['Set Wood', () => new SetWoodNode()],
                ['Multiply Energy', () => new MultiplyEnergyNode()],
                ['Multiply Water', () => new MultiplyWaterNode()],
                ['Set Energy', () => new SetEnergyNode()],
                ['Set Auxins', () => new SetAuxinsNode()],
                ['Set trySpawn', () => new SetTrySpawnNode()],
                ['Accumulate Production', () => new AccumulateProductionNode()],
                ['Make Bud', () => new MakeBudNode()],
                ['Create Leaves', () => new CreateLeavesNode()],
                ['Death', () => new DeathNode()],
                ['Death Parent', () => new DeathParentNode()],
                ['Death Children', () => new DeathChildrenNode()],
                ['Become Meristem', () => new BecomeMeristemNode()],
                ['Become Stem', () => new BecomeStemNode()],
                ['Become Flower Stem', () => new BecomeFlowerStemNode()],
                ['Become Flower Meristem', () => new BecomeFlowerMeristemNode()],
                ['Spawn Meristem', () => new SpawnMeristemNode()],
                ['Spawn Bud', () => new SpawnBudNode()],
                ['Spawn Stem', () => new SpawnStemNode()],
                ['Spawn Flower Stem', () => new SpawnFlowerStemNode()],
                ['Spawn Flower Meristem', () => new SpawnFlowerMeristemNode()],
                ['Spawn Flower Bud', () => new SpawnFlowerBudNode()],
                ['Spawn Flower Padel', () => new SpawnFlowerPadelNode()],
                ['Spawn Rhizome', () => new SpawnRhizomeNode()],
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
                ['Divide', () => new DivideNode()],
                ['Parent Wood Cap', () => new ParentWoodCapNode()],
                ['Clamp Max', () => new ClampMaxNode()],
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
            if ((c.data as any).label === 'Active') {
                const actives = editor.getNodes().filter((n: any) => n.label === 'Active');
                if (actives.length > 1)
                    setTimeout(() => editor.removeNode(c.data.id).catch(() => { }), 0);
            }
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

        if (c.type === 'noderemoved') {
            const removed = c.data;
            if ((removed as any).label === 'Active') {
                const still = editor.getNodes().some((n: any) => n.label === 'Active');
                if (!still) {
                    setTimeout(async () => {
                        const node = new ActiveOutputNode();
                        await editor.addNode(node);
                        await area.translate(node.id, { x: 120, y: 120 });
                        pushSpeciesGraph(species, namedGraph, editor, area);
                    }, 0);
                }
            }
        }

        if (['connectioncreated', 'connectionremoved', 'nodecreated', 'noderemoved'].includes(context.type)) {
            setTimeout(() => {
                pushSpeciesGraph(species, namedGraph, editor, area);
            }, 0);
        }

        return context;
    });

    import('./Controls').then(m => {
        m.graphUpdateTrigger.addEventListener('update', () => {
            setTimeout(() => {
                pushSpeciesGraph(species, namedGraph, editor, area);
            }, 0);
        });
    });

    editor.use(area);
    area.use(connection);
    area.use(renderPlugin);
    area.use(contextMenu);

    AreaExtensions.simpleNodesOrder(area);

    const initial = namedGraph.graph;
    if (initial?.nodes?.length > 0)
        await fromJSON(initial, editor, area, createNodeFromExport);

    const speciesName = species.name.peek();
    const graphId = namedGraph.id;
    appstate.registerBehaviorGraphGetter(speciesName, graphId, () => toJSON(editor, area));

    setTimeout(() => {
        const nodes = editor.getNodes();
        if (nodes.length > 0)
            AreaExtensions.zoomAt(area, nodes);
        pushSpeciesGraph(species, namedGraph, editor, area);
    }, 10);

    const handleKeyDown = (e: KeyboardEvent) => {
        if (e.key === 'Escape') {
            area.emit({ type: 'pointerdown', data: { event: new PointerEvent('pointerdown') } } as any);
        }
    };
    window.addEventListener('keydown', handleKeyDown);

    return {
        destroy: () => {
            appstate.unregisterBehaviorGraphGetter(speciesName, graphId);
            window.removeEventListener('keydown', handleKeyDown);
            area.destroy();
        },
        autoLayout: async () => {
            await applyAutoLayout(editor, area);
            pushSpeciesGraph(species, namedGraph, editor, area);
            const nodes = editor.getNodes();
            if (nodes.length > 0)
                AreaExtensions.zoomAt(area, nodes);
        },
    };
}

export default function BehaviorEditor({ species, namedGraph }: { species: Species; namedGraph: NamedGraph }) {
    const factory = useMemo(
        () => (container: HTMLElement) => createEditor(container, species, namedGraph),
        [species.name.value, namedGraph.id]
    );
    const [ref, editorApi] = useRete(factory);

    return (
        <div style={{ width: '100%', height: '100%', display: 'flex', flexDirection: 'column', background: 'rgba(0,0,0,0.1)' }}>
            <div style={{ flexShrink: 0, padding: '4px 8px', display: 'flex', gap: 8, alignItems: 'center' }}>
                <button
                    type="button"
                    title="Arrange nodes left-to-right by connections"
                    disabled={!editorApi}
                    onClick={() => editorApi?.autoLayout()}
                >
                    Auto-layout
                </button>
            </div>
            <div ref={ref} style={{ flex: 1, minHeight: 0, width: '100%' }} />
        </div>
    );
}