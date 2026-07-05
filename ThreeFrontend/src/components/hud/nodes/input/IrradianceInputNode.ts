import { ClassicPreset } from "rete";
import { numSocket } from "../Sockets";

export class IrradianceInputNode extends ClassicPreset.Node {
    constructor() {
        super("Irradiance Input");
        this.addOutput("irradiance", new ClassicPreset.Output(numSocket, "Irradiance"));
    }
}