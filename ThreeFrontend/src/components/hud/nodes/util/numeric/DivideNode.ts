import { ClassicPreset } from 'rete';
import { numSocket } from '../../Sockets';

export class DivideNode extends ClassicPreset.Node {
    constructor() {
        super('Divide');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }

    data(inputs: { a?: number[], b?: number[] }) {
        const a = inputs.a ? inputs.a[0] : 0;
        const b = inputs.b ? inputs.b[0] : 0;
        return { out: b !== 0 ? a / b : 0 };
    }
}
