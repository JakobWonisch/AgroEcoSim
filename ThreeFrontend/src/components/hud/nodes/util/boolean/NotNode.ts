import { ClassicPreset } from 'rete';
import { boolSocket } from '../../Sockets';

export class NotNode extends ClassicPreset.Node {
    constructor() {
        super('Not');
        this.addInput('a', new ClassicPreset.Input(boolSocket, 'A'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }
}