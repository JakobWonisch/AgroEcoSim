import { ClassicPreset } from 'rete';
import { boolSocket } from '../sockets';

export class AgentTypeNode extends ClassicPreset.Node {
    constructor() {
        super('Agent Type');
        
        // 5 different boolean output values
        this.addOutput('type1', new ClassicPreset.Output(boolSocket, 'Type 1'));
        this.addOutput('type2', new ClassicPreset.Output(boolSocket, 'Type 2'));
        this.addOutput('type3', new ClassicPreset.Output(boolSocket, 'Type 3'));
        this.addOutput('type4', new ClassicPreset.Output(boolSocket, 'Type 4'));
        this.addOutput('type5', new ClassicPreset.Output(boolSocket, 'Type 5'));
    }

    data() {
        // Randomly set outputs for now
        return {
            type1: Math.random() > 0.5,
            type2: Math.random() > 0.5,
            type3: Math.random() > 0.5,
            type4: Math.random() > 0.5,
            type5: Math.random() > 0.5,
        };
    }
}
