import { ClassicPreset } from 'rete';
import { numSocket } from '../../Sockets';

/** uint / uint division (matches legacy C# integer division for age ratios). */
export class IntegerDivideNode extends ClassicPreset.Node {
    constructor() {
        super('Integer Divide');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }
}
