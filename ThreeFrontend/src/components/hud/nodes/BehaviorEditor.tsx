// ─── src/components/app.tsx ───────────────────────────────────────────────────
import { h } from 'preact';
import { useCallback } from 'preact/hooks';
import ReactFlow, {
  addEdge,
  MiniMap,
  Controls,
  BackgroundVariant,
  useNodesState,
  useEdgesState,
  Connection,
  Node,
  Edge,
  Background,
} from 'reactflow';
import 'reactflow/dist/style.css';

const initialNodes: Node[] = [
  { id: '1', type: 'input', data: { label: 'Input' },   position: { x: 100, y: 100 } },
  { id: '2',                data: { label: 'Process' },  position: { x: 300, y: 200 } },
  { id: '3', type: 'output', data: { label: 'Output' }, position: { x: 500, y: 100 } },
];

const initialEdges: Edge[] = [
  { id: 'e1-2', source: '1', target: '2', animated: true },
  { id: 'e2-3', source: '2', target: '3' },
];

export default function BehaviorEditor() {
  const [nodes, , onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  const onConnect = useCallback(
    (connection: Connection) => setEdges(eds => addEdge(connection, eds)),
    [setEdges]
  );

  return (
    <div style={{ width: '100vw', height: '100vh' }}>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        fitView
      >
        <MiniMap />
        <Controls />
        <Background variant={BackgroundVariant.Dots} gap={16} size={1} />
      </ReactFlow>
    </div>
  );
}