import {
    ClassicPreset,
    GetSchemes
} from 'rete';
import { ContextMenuExtra } from 'rete-context-menu-plugin';
import { ReactArea2D } from 'rete-react-plugin';

export type Node = ClassicPreset.Node;
export type Conn = ClassicPreset.Connection<Node, Node>;
export type Schemes = GetSchemes<Node, Conn>;
export type AreaExtra = ReactArea2D<Schemes> | ContextMenuExtra;