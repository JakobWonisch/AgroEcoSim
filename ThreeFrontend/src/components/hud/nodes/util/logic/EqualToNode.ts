import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../sockets';

export class EqualToNode extends ClassicPreset.Node {
    constructor() {
        super('Equal To');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }

    data(inputs: { a?: number[], b?: number[] }) {
        const a = inputs.a ? inputs.a[0] : 0;
        const b = inputs.b ? inputs.b[0] : 0;
        return { out: a === b };
    }
}
