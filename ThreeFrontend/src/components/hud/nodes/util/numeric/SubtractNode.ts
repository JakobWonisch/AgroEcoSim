import { ClassicPreset } from 'rete';
import { numSocket } from '../../Sockets';

export class SubtractNode extends ClassicPreset.Node {
    constructor() {
        super('Subtract');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }
}