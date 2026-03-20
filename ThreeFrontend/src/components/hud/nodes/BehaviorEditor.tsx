import { h } from 'preact';
import { useEffect, useRef } from 'preact/hooks';
import { ClassicPreset, GetSchemes, NodeEditor } from 'rete';
import { AreaPlugin } from 'rete-area-plugin';
import { ConnectionPlugin } from 'rete-connection-plugin';
import { Presets, ReactArea2D, ReactPlugin } from 'rete-react-plugin';
import { createRoot } from '../../../react-dom-client-shim';

type Schemes = GetSchemes<
    ClassicPreset.Node,
    ClassicPreset.Connection<ClassicPreset.Node, ClassicPreset.Node>
>;
type AreaExtra = ReactArea2D<Schemes>;

export default function BehaviorEditor() {
    const container = useRef(null);

    useEffect(() => {
        if (!container.current) return;

        const editor = new NodeEditor<Schemes>();


        const area = new AreaPlugin<Schemes, AreaExtra>(container.current);
        const render = new ReactPlugin<Schemes, AreaExtra>({ createRoot });

        render.addPreset(Presets.classic.setup());

        const connection = new ConnectionPlugin<Schemes>();
        editor.use(area);
        // editor.use(area);
        // editor.use(connection);
        area.use(render);


        const addNode = async () => {
            // Example node
            const socket = new ClassicPreset.Socket("socket");

            const nodeA = new ClassicPreset.Node("A");
            nodeA.addControl("a", new ClassicPreset.InputControl("text", {}));
            nodeA.addOutput("a", new ClassicPreset.Output(socket));
            await editor.addNode(nodeA);

            area.translate(nodeA.id, { x: 100, y: 100 });
        };

        addNode().catch(err => console.log("error: ", err));

        return () => {
            // editor.destroy();
        };
    }, []);

    return <div ref={container} className="node-container" style={{ width: '800px', height: '900px', backgroundColor: 'gray' }} />;
}