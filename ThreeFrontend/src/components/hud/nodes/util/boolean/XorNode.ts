import { ClassicPreset } from 'rete';
import { boolSocket } from '../../Sockets';

export class XorNode extends ClassicPreset.Node {
    constructor() {
        super('Xor');
        this.addInput('a', new ClassicPreset.Input(boolSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(boolSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }

    data(inputs: { a?: boolean[], b?: boolean[] }) {
        const a = inputs.a ? inputs.a[0] : false;
        const b = inputs.b ? inputs.b[0] : false;
        return { out: (a && !b) || (!a && b) };
    }
}
