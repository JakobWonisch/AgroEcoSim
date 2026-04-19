import { ClassicPreset } from 'rete';
import { boolSocket } from '../../sockets';

export class NotNode extends ClassicPreset.Node {
    constructor() {
        super('Not');
        this.addInput('a', new ClassicPreset.Input(boolSocket, 'A'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }

    data(inputs: { a?: boolean[] }) {
        const a = inputs.a ? inputs.a[0] : false;
        return { out: !a };
    }
}
