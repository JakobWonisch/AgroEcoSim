import { ClassicPreset } from 'rete';
import { numSocket } from '../sockets';

export class NumberOutputNode extends ClassicPreset.Node {
    constructor() {
        super('Number Output');
        this.addInput('num', new ClassicPreset.Input(numSocket, 'Number'));
    }

    data() {
        return {};
    }
}
