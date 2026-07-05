import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../Sockets';

export class LessThanNode extends ClassicPreset.Node {
    constructor() {
        super('Less Than');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }
}
