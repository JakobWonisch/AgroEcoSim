import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../Sockets';

export class LessThanNode extends ClassicPreset.Node {
    constructor() {
        super('Less Than (or Equal)');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addControl('equal', new ClassicPreset.InputControl('number', { initial: 0 })); // 0 = false, 1 = true
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }
}