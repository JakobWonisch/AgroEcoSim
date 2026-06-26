import { ClassicPreset } from 'rete';
import { numSocket } from '../../Sockets';

export class DivideNode extends ClassicPreset.Node {
    constructor() {
        super('Divide');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }
}