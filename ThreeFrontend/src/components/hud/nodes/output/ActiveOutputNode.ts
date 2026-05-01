import { ClassicPreset } from 'rete';
import { boolSocket } from '../Sockets';

export class ActiveOutputNode extends ClassicPreset.Node {
    constructor() {
        super('Active');
        this.addInput('isActive', new ClassicPreset.Input(boolSocket, 'Is Active'));
    }

    data() {
        return {};
    }
}
