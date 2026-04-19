import { h, render } from 'preact';
import {
    NodeEditor, GetSchemes, ClassicPreset
} from 'rete';
import { AreaPlugin, AreaExtensions } from 'rete-area-plugin';
import { ConnectionPlugin, Presets as ConnectionPresets } from 'rete-connection-plugin';
import { ReactPlugin, Presets, ReactArea2D, useRete } from 'rete-react-plugin';
import { SwitchControl, SwitchControlComponent, CustomInputComponent, CustomSocketComponent } from './controls';
import { NumberInputNode } from './input/NumberInputNode';
import { BooleanInputNode } from './input/BooleanInputNode';
import { GreaterThanNode } from './util/logic/GreaterThanNode';
import { AndNode } from './util/boolean/AndNode';
import { BooleanOutputNode } from './output/BooleanOutputNode';

type Node = ClassicPreset.Node;
type Conn = ClassicPreset.Connection<Node, Node>;
type Schemes = GetSchemes<Node, Conn>;
type AreaExtra = ReactArea2D<Schemes>;

export const DEBUG_SHOW_VALUES = true;

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

export async function createEditor(container: HTMLElement) {
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
    connection.addPreset(ConnectionPresets.classic.setup());

    editor.addPipe(context => {
        if (context.type === 'connectioncreate') {
            const { source, target, sourceOutput, targetInput } = context.data;
            const sourceNode = editor.getNode(source);
            const targetNode = editor.getNode(target);
            
            // Get the socket definitions to check compatibility
            const outSocket = sourceNode?.outputs[sourceOutput]?.socket;
            const inSocket = targetNode?.inputs[targetInput]?.socket;

            if (outSocket && inSocket && outSocket.name !== inSocket.name) {
                // Prevent the connection if sockets mismatch
                return;
            }
        }

        if (['connectioncreated', 'connectionremoved', 'nodecreated', 'noderemoved'].includes(context.type)) {
            setTimeout(() => processGraph(editor, area), 0);
        }

        return context;
    });

    import('./controls').then(m => {
        m.graphUpdateTrigger.addEventListener('update', () => {
            setTimeout(() => processGraph(editor, area), 0);
        });
    });

    editor.use(area);
    area.use(connection);
    area.use(renderPlugin);

    AreaExtensions.simpleNodesOrder(area);

    const valInput = new NumberInputNode(10);
    valInput.label = 'Value';
    await editor.addNode(valInput);

    const threshInput = new NumberInputNode(5);
    threshInput.label = 'Threshold';
    await editor.addNode(threshInput);

    const activeSwitch = new BooleanInputNode(true);
    await editor.addNode(activeSwitch);

    const gtNode = new GreaterThanNode();
    await editor.addNode(gtNode);

    const andNode = new AndNode();
    await editor.addNode(andNode);

    const outNode = new BooleanOutputNode();
    await editor.addNode(outNode);

    await editor.addConnection(new ClassicPreset.Connection<Node, Node>(valInput, 'num', gtNode, 'a'));
    await editor.addConnection(new ClassicPreset.Connection<Node, Node>(threshInput, 'num', gtNode, 'b'));
    
    await editor.addConnection(new ClassicPreset.Connection<Node, Node>(gtNode, 'out', andNode, 'a'));
    await editor.addConnection(new ClassicPreset.Connection<Node, Node>(activeSwitch, 'bool', andNode, 'b'));

    await editor.addConnection(new ClassicPreset.Connection<Node, Node>(andNode, 'out', outNode, 'bool'));

    await area.translate(valInput.id, { x: 50, y: 50 });
    await area.translate(threshInput.id, { x: 50, y: 250 });
    await area.translate(activeSwitch.id, { x: 50, y: 450 });

    await area.translate(gtNode.id, { x: 350, y: 150 });
    await area.translate(andNode.id, { x: 650, y: 300 });
    await area.translate(outNode.id, { x: 950, y: 300 });

    setTimeout(() => {
        AreaExtensions.zoomAt(area, editor.getNodes());
        processGraph(editor, area);
    }, 10);

    return {
        destroy: () => area.destroy()
    };
}

export default function BehaviorEditor() {
    const [ref] = useRete(createEditor);

    return (
        <div style={{ width: '100%', height: '100%', background: 'rgba(0,0,0,0.1)' }}>
            <div ref={ref} style={{ width: '100%', height: '100%' }} />
        </div>
    );
}