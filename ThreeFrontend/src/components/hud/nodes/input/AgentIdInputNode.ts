import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class AgentIdInputNode extends ClassicPreset.Node {
    constructor() {
        super("Agent Id Input");
        this.addOutput("agentId", new ClassicPreset.Output(numSocket, "Agent id"));
    }
}
