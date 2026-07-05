import { ClassicPreset } from 'rete';
import { boolSocket } from '../../Sockets';

export class OrNode extends ClassicPreset.Node {
    constructor() {
        super('Or');
        this.addInput('a', new ClassicPreset.Input(boolSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(boolSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }
}