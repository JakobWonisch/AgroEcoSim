import { h, render } from 'preact';
import {
  NodeEditor, GetSchemes, ClassicPreset
} from 'rete';
import { AreaPlugin, AreaExtensions } from 'rete-area-plugin';
import { ConnectionPlugin, Presets as ConnectionPresets } from 'rete-connection-plugin';
import { ReactPlugin, Presets, ReactArea2D, useRete } from 'rete-react-plugin';

type Node = ClassicPreset.Node;
type Conn = ClassicPreset.Connection<Node, Node>;
type Schemes = GetSchemes<Node, Conn>;
type AreaExtra = ReactArea2D<Schemes>;

export async function createEditor(container: HTMLElement) {
    const socket = new ClassicPreset.Socket('socket');

    const editor = new NodeEditor<Schemes>();
    const area = new AreaPlugin<Schemes, AreaExtra>(container);
    const connection = new ConnectionPlugin<Schemes, AreaExtra>();
    
    // Custom createRoot wrapper for Preact
    const createRoot = (container: HTMLElement) => ({
        render: (element: any) => render(element as any, container),
        unmount: () => render(null, container)
    });

    const renderPlugin = new ReactPlugin<Schemes, AreaExtra>({ createRoot });

    renderPlugin.addPreset(Presets.classic.setup());
    connection.addPreset(ConnectionPresets.classic.setup());

    editor.use(area);
    area.use(connection);
    area.use(renderPlugin);

    AreaExtensions.simpleNodesOrder(area);

    const a = new ClassicPreset.Node('Input');
    a.addOutput('a', new ClassicPreset.Output(socket));
    await editor.addNode(a);

    const b = new ClassicPreset.Node('Process');
    b.addInput('a', new ClassicPreset.Input(socket));
    b.addOutput('b', new ClassicPreset.Output(socket));
    await editor.addNode(b);

    const c = new ClassicPreset.Node('Output');
    c.addInput('b', new ClassicPreset.Input(socket));
    await editor.addNode(c);

    await editor.addConnection(new ClassicPreset.Connection(a, 'a', b, 'a'));
    await editor.addConnection(new ClassicPreset.Connection(b, 'b', c, 'b'));

    await area.translate(a.id, { x: 100, y: 100 });
    await area.translate(b.id, { x: 300, y: 200 });
    await area.translate(c.id, { x: 500, y: 100 });

    setTimeout(() => {
        AreaExtensions.zoomAt(area, editor.getNodes());
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