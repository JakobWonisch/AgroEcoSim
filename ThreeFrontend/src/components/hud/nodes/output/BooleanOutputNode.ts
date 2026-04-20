import { ClassicPreset } from 'rete';
import { boolSocket } from '../Sockets';

export class BooleanOutputNode extends ClassicPreset.Node {
    constructor() {
        super('Boolean Output');
        this.addInput('bool', new ClassicPreset.Input(boolSocket, 'Boolean'));
    }

    data() {
        return {};
    }
}
