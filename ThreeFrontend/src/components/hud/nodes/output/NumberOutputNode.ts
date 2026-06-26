import { ClassicPreset } from 'rete';
import { numSocket } from '../Sockets';

export class NumberOutputNode extends ClassicPreset.Node {
    constructor() {
        super('Number Output');
        this.addInput('num', new ClassicPreset.Input(numSocket, 'Number'));
    }
}