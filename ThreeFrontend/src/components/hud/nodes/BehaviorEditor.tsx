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
import { CommentableNodeComponent } from './CommentableNode';
import {
    AddToConfigControl,
    AddToConfigControlComponent,
    ConfigSelectControl,
    ConfigSelectControlComponent,
} from './ConfigurationControls';
import { CustomInputComponent, CustomSocketComponent, SwitchControl, SwitchControlComponent } from './Controls';
import { AgentTypeInputNode } from './input/AgentTypeInputNode';
import { PhaseInputNode } from './input/PhaseInputNode';
import { AgentStateInputNode } from './input/AgentStateInputNode';
import { FormationInputNode } from './input/FormationInputNode';
import { IrradianceInputNode } from './input/IrradianceInputNode';
import { SimulationSettingsInputNode } from './input/SimulationSettingsInputNode';
import { RandomChanceInputNode } from './input/RandomChanceInputNode';
import { BooleanInputNode } from './input/BooleanInputNode';
import { NumberInputNode } from './input/NumberInputNode';
import { ConfigurationValueInputNode } from './input/ConfigurationValueInputNode';
import { AreaExtra, Schemes } from './NodeTypes';
import { ActiveOutputNode } from './output/ActiveOutputNode';
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
import { AccumulateEnvResourcesNode } from './output/AccumulateEnvResourcesNode';
import { AccumulateEnvResourcesInvNode } from './output/AccumulateEnvResourcesInvNode';
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
import { GreaterThanOrEqualNode } from './util/logic/GreaterThanOrEqualNode';
import { IfElseNode } from './util/logic/IfElseNode';
import { LessThanNode } from './util/logic/LessThanNode';
import { LessThanOrEqualNode } from './util/logic/LessThanOrEqualNode';
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
import { setEditorContext } from './editorContext';
import { notifyGraphUiUpdate } from './graphUpdate';

function pushSpeciesGraph(species: Species, namedGraph: NamedGraph, editor: NodeEditor<Schemes>, area: AreaPlugin<Schemes, AreaExtra>) {
    // Avoid overwriting node positions with 0/0 snapshots before views are ready.
    const nodes = editor.getNodes();
    if (nodes.some((n: any) => !area.nodeViews.get(n.id)))
        return;

    const snapshot = toJSON(editor, area);
    species.behaviorGraphs.value = species.behaviorGraphs.peek().map(g =>
        g.id === namedGraph.id ? { ...g, graph: snapshot } : g);
}

function schedulePushSpeciesGraph(
    species: Species,
    namedGraph: NamedGraph,
    editor: NodeEditor<Schemes>,
    area: AreaPlugin<Schemes, AreaExtra>,
    suppressGraphPush: () => boolean,
) {
    if (suppressGraphPush())
        return;
    setTimeout(() => pushSpeciesGraph(species, namedGraph, editor, area), 0);
}

function measureNodeView(area: AreaPlugin<Schemes, AreaExtra>, nodeId: string) {
    const el = area.nodeViews.get(nodeId)?.element;
    if (el && el.offsetWidth > 0 && el.offsetHeight > 0) {
        return { width: el.offsetWidth, height: el.offsetHeight };
    }
    return { width: 200, height: 80 };
}

async function placeNodeTopCenterAt(
    area: AreaPlugin<Schemes, AreaExtra>,
    nodeId: string,
    anchor: { x: number; y: number },
) {
    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    const { width } = measureNodeView(area, nodeId);
    await area.translate(nodeId, { x: anchor.x - width / 2, y: anchor.y });
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
            node() {
                return CommentableNodeComponent as any;
            },
            control(data) {
                if (data.payload instanceof AddToConfigControl) {
                    return AddToConfigControlComponent as any;
                }
                if (data.payload instanceof ConfigSelectControl) {
                    return ConfigSelectControlComponent as any;
                }
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

    const behaviorGraphMenuItems = ContextMenuPresets.classic.setup([
            ['input constant', [
                ['Number Input', () => new NumberInputNode(0)],
                ['Boolean Input', () => new BooleanInputNode(false)],
                ['Configuration Value Input', () => new ConfigurationValueInputNode()],
            ]],
            ['input dynamic', [
                ['Agent Type Input', () => new AgentTypeInputNode()],
                ['Phase Input', () => new PhaseInputNode()],
                ['Agent State Input', () => new AgentStateInputNode()],
                ['Formation Input', () => new FormationInputNode()],
                ['Irradiance Input', () => new IrradianceInputNode()],
                ['Simulation Settings Input', () => new SimulationSettingsInputNode()],
                ['Random Chance Input', () => new RandomChanceInputNode()],
            ]],
            ['output delta', [
                ['Growth', () => new GrowthNode()],
                ['Delta Energy', () => new DeltaEnergyNode()],
                ['Delta Water', () => new DeltaWaterNode()],
                ['Delta Wood', () => new DeltaWoodNode()],
            ]],
            ['output set', [
                ['Set Wood', () => new SetWoodNode()],
                ['Multiply Energy', () => new MultiplyEnergyNode()],
                ['Multiply Water', () => new MultiplyWaterNode()],
                ['Set Energy', () => new SetEnergyNode()],
                ['Set Auxins', () => new SetAuxinsNode()],
                ['Set trySpawn', () => new SetTrySpawnNode()],
                ['Accumulate Production', () => new AccumulateProductionNode()],
                ['Accumulate Env Resources', () => new AccumulateEnvResourcesNode()],
                ['Accumulate Env Resources Inv', () => new AccumulateEnvResourcesInvNode()],
                ['Make Bud', () => new MakeBudNode()],
                ['Create Leaves', () => new CreateLeavesNode()],
                ['Become Meristem', () => new BecomeMeristemNode()],
                ['Become Stem', () => new BecomeStemNode()],
                ['Become Flower Stem', () => new BecomeFlowerStemNode()],
                ['Become Flower Meristem', () => new BecomeFlowerMeristemNode()],
            ]],
            ['output spawn', [
                ['Spawn Meristem', () => new SpawnMeristemNode()],
                ['Spawn Bud', () => new SpawnBudNode()],
                ['Spawn Stem', () => new SpawnStemNode()],
                ['Spawn Flower Stem', () => new SpawnFlowerStemNode()],
                ['Spawn Flower Meristem', () => new SpawnFlowerMeristemNode()],
                ['Spawn Flower Bud', () => new SpawnFlowerBudNode()],
                ['Spawn Flower Padel', () => new SpawnFlowerPadelNode()],
                ['Spawn Rhizome', () => new SpawnRhizomeNode()],
            ]],
            ['output death', [
                ['Death', () => new DeathNode()],
                ['Death Parent', () => new DeathParentNode()],
                ['Death Children', () => new DeathChildrenNode()],
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
                ['Greater Than', () => new GreaterThanNode()],
                ['Greater Than or Equal', () => new GreaterThanOrEqualNode()],
                ['Less Than', () => new LessThanNode()],
                ['Less Than or Equal', () => new LessThanOrEqualNode()],
                ['Equal To', () => new EqualToNode()],
                ['If / Else', () => new IfElseNode()]
            ]]
        ]);

    const contextMenu = new ContextMenuPlugin<Schemes>({
        items: (context, plugin) => {
            const menu = behaviorGraphMenuItems(context, plugin);
            return context === 'root' ? { ...menu, searchBar: false } : menu;
        },
    });

    connection.addPreset(ConnectionPresets.classic.setup());

    let pendingNodePosition: { x: number, y: number } | null = null;
    let suppressGraphPush = false;
    const isGraphPushSuppressed = () => suppressGraphPush;
    const schedulePush = () => schedulePushSpeciesGraph(species, namedGraph, editor, area, isGraphPushSuppressed);

    area.addPipe(context => {
        const c = context as any;
        if (c.type === 'contextmenu' && c.data.context === 'root') {
            area.area.setPointerFrom(c.data.event);
            pendingNodePosition = { ...area.area.pointer };
        }
        if (c.type === 'pointerdown') {
            pendingNodePosition = null;
        }
        if (c.type === 'nodetranslated') {
            schedulePush();
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
            if (pendingNodePosition) {
                const anchor = { ...pendingNodePosition };
                pendingNodePosition = null;
                setTimeout(() => { void placeNodeTopCenterAt(area, c.data.id, anchor); }, 0);
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
            schedulePush();
            if (context.type === 'connectioncreated' || context.type === 'connectionremoved')
                setTimeout(() => notifyGraphUiUpdate(), 0);
        }

        return context;
    });

    import('./graphUpdate').then(m => {
        m.graphUpdateTrigger.addEventListener('update', schedulePush);
    });

    editor.use(area);
    area.use(connection);
    area.use(renderPlugin);
    area.use(contextMenu);

    AreaExtensions.simpleNodesOrder(area);

    const initial = namedGraph.graph;
    if (initial?.nodes?.length > 0) {
        suppressGraphPush = true;
        try {
            await fromJSON(initial, editor, area, createNodeFromExport);
        } finally {
            suppressGraphPush = false;
        }
    }

    const speciesName = species.name.peek();
    const graphId = namedGraph.id;
    appstate.registerBehaviorGraphGetter(speciesName, graphId, () => toJSON(editor, area));

    setEditorContext({
        species,
        editor,
        area,
        pushGraph: () => pushSpeciesGraph(species, namedGraph, editor, area),
    });

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
            pushSpeciesGraph(species, namedGraph, editor, area);
            setEditorContext(null);
            appstate.unregisterBehaviorGraphGetter(speciesName, graphId);
            window.removeEventListener('keydown', handleKeyDown);
            area.destroy();
        },
        autoLayout: async () => {
            suppressGraphPush = true;
            try {
                await applyAutoLayout(editor, area);
            } finally {
                suppressGraphPush = false;
            }
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