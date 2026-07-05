import { ClassicPreset } from 'rete';
import { boolSocket } from '../../Sockets';

export class XorNode extends ClassicPreset.Node {
    constructor() {
        super('Xor');
        this.addInput('a', new ClassicPreset.Input(boolSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(boolSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }
}