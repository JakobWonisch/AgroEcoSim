import type { NodeEditor } from 'rete';
import type { AreaPlugin } from 'rete-area-plugin';
import type { Species } from '../../../helpers/Species';
import type { AreaExtra, Schemes } from './NodeTypes';

export type EditorContext = {
    species: Species;
    editor: NodeEditor<Schemes>;
    area: AreaPlugin<Schemes, AreaExtra>;
    pushGraph: () => void;
};

let activeEditorContext: EditorContext | null = null;

export function setEditorContext(ctx: EditorContext | null) {
    activeEditorContext = ctx;
}

export function getEditorContext(): EditorContext | null {
    return activeEditorContext;
}
