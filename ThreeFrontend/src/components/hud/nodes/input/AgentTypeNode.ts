import { ClassicPreset } from 'rete';
import { boolSocket } from '../Sockets';

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
        // Editor preview only; simulation uses live organ flags on the server (same mapping as Organ Sensors).
        return {
            type1: false,
            type2: false,
            type3: false,
            type4: false,
            type5: false,
        };
    }
}
