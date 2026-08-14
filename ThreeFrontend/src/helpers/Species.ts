import { signal } from "@preact/signals"
import type { ExportedGraph, NamedGraph } from "../components/hud/nodes/Conversion";
import { createDefaultNamedGraph } from "../components/hud/nodes/Conversion";
import type { BehaviorConfigEntry } from "../components/hud/nodes/behaviorConfiguration";
import { fromWireEntries, toWireEntries } from "../components/hud/nodes/behaviorConfiguration";

const DegToRad = Math.PI / 180.0;
const RadToDeg = 180.0 / Math.PI;

/** Morphology-only predefined literals not represented in behavior configuration. */
const PREDEFINED_MORPHOLOGY_SUPPLEMENTS: Record<string, Partial<{
    height: number;
    leafGrowthTime: number;
    lateralsPerNode: number;
}>> = {
    "Persea americana": { height: 12, leafGrowthTime: 720, lateralsPerNode: 4 },
    "Geranium Macrorrhizum": { height: 0.3, leafGrowthTime: 24 * 7 },
    "Geranium × Cantabrigiense": { height: 0.25, leafGrowthTime: 24 * 7 },
    "Bergenia Cordifolia": { height: 0.04, leafGrowthTime: 24 * 7 * 12 },
};

/** Config ids from DefaultSpeciesGraphBuilder / BerganiaTickGraphBuilder. */
const ConfigIds = {
    leafLength: "default-config-leaf-length",
    leafRadius: "default-config-leaf-radius",
    petioleLength: "default-config-petiole-length",
    petioleRadius: "default-config-petiole-radius",
    nodeDistance: "default-config-node-distance",
    nodeDistanceVar: "default-config-node-distance-var",
    monopodialFactor: "default-config-monopodial-factor",
    dominanceFactor: "default-config-dominance-factor",
    auxinsProduction: "default-config-auxins-production",
    lateralsPerNode: "default-config-laterals-per-node",
    lateralRoll: "default-config-lateral-roll",
    lateralRollVar: "default-config-lateral-roll-var",
    lateralPitch: "default-config-lateral-pitch",
    lateralPitchVar: "default-config-lateral-pitch-var",
    leafPitch: "default-config-leaf-pitch",
    twigsBending: "default-config-twig-bending",
    twigsBendingLevel: "default-config-twig-bending-level",
    twigsBendingApical: "default-config-twig-bending-apical",
    woodGrowthTime: "default-config-wood-growth-time",
    woodGrowthTimeVar: "default-config-wood-growth-time-var",
    rizomeLength: "default-config-rizome-length",
    rizomeRadius: "default-config-rizome-radius",
    petioleAgeBudMinHours: "default-config-petiole-age-bud-min-hours",
    floweringStartAgeHours: "default-config-flowering-start-age-hours",
    floweringEndAgeHours: "default-config-flowering-end-age-hours",
    growthFactor: "default-config-growth-factor",
    maxRadius: "default-config-max-radius",
    pNewCrown: "default-config-p-new-crown",
    pExpandRizome: "default-config-p-expand-rizome",
    rizomeMaxDepth: "default-config-rizome-max-depth",
    crownPitch: "default-config-crown-pitch",
    pChaining: "default-config-p-chaining",
    pFlowering: "default-config-p-flowering",
} as const;

function configNumber(entries: BehaviorConfigEntry[], id: string): number | undefined {
    const entry = entries.find(e => e.id === id);
    return entry?.type === "number" && typeof entry.value === "number" ? entry.value : undefined;
}

function configArray(entries: BehaviorConfigEntry[], id: string): number[] | undefined {
    const entry = entries.find(e => e.id === id);
    return entry?.type === "number[]" && Array.isArray(entry.value)
        ? entry.value.map(v => Number(v) || 0)
        : undefined;
}

/** SpeciesSettings fields that only exist on the configuration panel (no HUD sliders). */
function configBackedSpeciesSettings(entries: BehaviorConfigEntry[]): Record<string, number | number[]> {
    const n = (id: string) => configNumber(entries, id);
    const a = (id: string) => configArray(entries, id);
    const out: Record<string, number | number[]> = {};
    const setNum = (key: string, id: string) => {
        const v = n(id);
        if (v !== undefined) out[key] = v;
    };
    const setArr = (key: string, id: string) => {
        const v = a(id);
        if (v !== undefined && v.length > 0) out[key] = v;
    };
    setNum("RizomeLength", ConfigIds.rizomeLength);
    setNum("RizomeRadius", ConfigIds.rizomeRadius);
    setNum("FloweringStartAgeHours", ConfigIds.floweringStartAgeHours);
    setNum("FloweringEndAgeHours", ConfigIds.floweringEndAgeHours);
    setNum("growthFactor", ConfigIds.growthFactor);
    setNum("MaxRadius", ConfigIds.maxRadius);
    setNum("pNewCrown", ConfigIds.pNewCrown);
    setNum("pExpandRizome", ConfigIds.pExpandRizome);
    setNum("RizomeMaxDepth", ConfigIds.rizomeMaxDepth);
    setNum("crownPitch", ConfigIds.crownPitch);
    setArr("pChaningSeaonns", ConfigIds.pChaining);
    setArr("pFloweringSeaonns", ConfigIds.pFlowering);
    // Bergania reuses petiole-age-bud-min-hours as MaxLeaveAge.
    if (n(ConfigIds.pExpandRizome) !== undefined)
        setNum("MaxLeaveAge", ConfigIds.petioleAgeBudMinHours);
    return out;
}

function applyBehaviorConfigurationToMorphology(species: Species, entries: BehaviorConfigEntry[]) {
    const n = (id: string) => configNumber(entries, id);
    const radToDeg = (v: number) => v * RadToDeg;

    if (n(ConfigIds.leafLength) !== undefined) species.leafLength.value = n(ConfigIds.leafLength)!;
    if (n(ConfigIds.leafRadius) !== undefined) species.leafRadius.value = n(ConfigIds.leafRadius)!;
    if (n(ConfigIds.petioleLength) !== undefined) species.petioleLength.value = n(ConfigIds.petioleLength)!;
    if (n(ConfigIds.petioleRadius) !== undefined) species.petioleRadius.value = n(ConfigIds.petioleRadius)!;
    if (n(ConfigIds.nodeDistance) !== undefined) species.nodeDistance.value = n(ConfigIds.nodeDistance)!;
    if (n(ConfigIds.nodeDistanceVar) !== undefined) species.nodeDistanceVar.value = n(ConfigIds.nodeDistanceVar)!;
    if (n(ConfigIds.monopodialFactor) !== undefined) species.monopodialFactor.value = n(ConfigIds.monopodialFactor)!;
    if (n(ConfigIds.dominanceFactor) !== undefined) species.dominanceFactor.value = n(ConfigIds.dominanceFactor)!;
    if (n(ConfigIds.auxinsProduction) !== undefined) species.auxinsProduction.value = n(ConfigIds.auxinsProduction)!;
    if (n(ConfigIds.lateralsPerNode) !== undefined) species.lateralsPerNode.value = n(ConfigIds.lateralsPerNode)!;
    if (n(ConfigIds.lateralRoll) !== undefined) species.lateralRollDeg.value = radToDeg(n(ConfigIds.lateralRoll)!);
    if (n(ConfigIds.lateralRollVar) !== undefined) species.lateralRollDegVar.value = radToDeg(n(ConfigIds.lateralRollVar)!);
    if (n(ConfigIds.lateralPitch) !== undefined) species.lateralPitchDeg.value = radToDeg(n(ConfigIds.lateralPitch)!);
    if (n(ConfigIds.lateralPitchVar) !== undefined) species.lateralPitchDegVar.value = radToDeg(n(ConfigIds.lateralPitchVar)!);
    if (n(ConfigIds.leafPitch) !== undefined) species.leafPitchDeg.value = radToDeg(n(ConfigIds.leafPitch)!);
    if (n(ConfigIds.twigsBending) !== undefined) species.twigsBending.value = n(ConfigIds.twigsBending)!;
    if (n(ConfigIds.twigsBendingLevel) !== undefined) species.bendingByLevel.value = n(ConfigIds.twigsBendingLevel)!;
    if (n(ConfigIds.twigsBendingApical) !== undefined) species.twigsBendingApical.value = n(ConfigIds.twigsBendingApical)!;
    // ShootsGravitaxis in graph config is the post-Init effective value (0.08); morphology Init applies ×0.4 again.
    if (n(ConfigIds.woodGrowthTime) !== undefined) species.woodGrowthTime.value = n(ConfigIds.woodGrowthTime)!;
    if (n(ConfigIds.woodGrowthTimeVar) !== undefined) species.woodGrowthTimeVar.value = n(ConfigIds.woodGrowthTimeVar)!;

    const supplement = PREDEFINED_MORPHOLOGY_SUPPLEMENTS[species.name.peek()];
    if (supplement?.height !== undefined) species.height.value = supplement.height;
    if (supplement?.leafGrowthTime !== undefined) species.leafGrowthTime.value = supplement.leafGrowthTime;
    if (supplement?.lateralsPerNode !== undefined) species.lateralsPerNode.value = supplement.lateralsPerNode;
}

export class Species {
    name = signal("Planta Fortuita " + Date.now());
    aka = signal("");
    behaviorIndex = signal(0);

    /** Ordered behavior graphs for simulation (Rete export); executed top-to-bottom per agent tick. */
    behaviorGraphs = signal<NamedGraph[]>([createDefaultNamedGraph("Main")]);

    /** Shared configuration values referenced by Configuration Value Input nodes. */
    behaviorConfiguration = signal<BehaviorConfigEntry[]>([]);

    //trunkToWood = signal(1);
    height = signal(12);

    nodeDistance = signal(0.04);
    nodeDistanceVar = signal(0.01);

    //https://sites.google.com/view/plant-diversity/
    monopodialFactor = signal(1); //at 0 it is fully dipodial, between 0 and 1 it is anisotomous see https://sites.google.com/site/paleoplant/terminology/branching
    dominanceFactor = signal(0.7);
    auxinsProduction = signal(40);
    auxinsReach = signal(1);
    lateralsPerNode = signal(2);
    lateralRollDeg = signal(0); //opposite = 0, alternate = 180, others are possible as well
    lateralRollDegVar = signal(5);
    lateralPitchDeg = signal(45);
    lateralPitchDegVar = signal(5);
    twigsBending = signal(0.5);
    twigsBendingApical = signal(0.02);
    bendingByLevel = signal(1);
    shootsGravitaxis = signal(0.2);

    woodGrowthTime = signal(100);
    woodGrowthTimeVar = signal(10);

    //leafLevel = signal(2);
    leafLength = signal(0.12);
    leafLengthVar = signal(0.02);
    leafRadius = signal(0.04);
    leafRadiusVar = signal(0.01);
    leafGrowthTime = signal(480);
    leafGrowthTimeVar = signal(120);
    leafPitchDeg = signal(20);
    leafPitchDegVar = signal(5);

    petioleLength = signal(0.05);
    petioleLengthVar = signal(0.01);
    petioleRadius = signal(0.0015);
    petioleRadiusVar = signal(0.0005);
    // RootLengthGrowthPerH = 0.023148148f,
    // RootRadiusGrowthPerH = 0.00297619f,

    rootsDensity = signal(0.5);
    rootsGravitaxis = signal(0.2);

    includeInRndGen = signal(true);

    public static Default() {
        const result = new Species();
        result.name.value = "default";
        return result;
    }

    public save() {
        return {
            name: this.name.peek(),
            aka: this.aka.peek(),
            graphs: structuredClone(this.behaviorGraphs.peek()),
            configuration: toWireEntries(this.behaviorConfiguration.peek()),
            behavior: this.behaviorIndex.peek(),
            height: this.height.peek(),

            nodeDistance: this.nodeDistance.peek(),
            nodeDistanceVar: this.nodeDistanceVar.peek(),

            monopodialFactor: this.monopodialFactor.peek(),
            dominanceFactor: this.dominanceFactor.peek(),

            auxinsProduction: this.auxinsProduction.peek(),
            auxinsReach: this.auxinsReach.peek(),

            lateralsPerNode: this.lateralsPerNode.peek(),
            lateralRoll: this.lateralRollDeg.peek() * DegToRad,
            lateralRollVar: this.lateralRollDegVar.peek() * DegToRad,
            lateralPitch: this.lateralPitchDeg.peek() * DegToRad,
            lateralPitchVar: this.lateralPitchDegVar.peek() * DegToRad,

            twigsBending: this.twigsBending.peek(),
            twigsBendingApical: this.twigsBendingApical.peek(),
            bendingByLevel: this.bendingByLevel.peek(),
            shootsGravitaxis: this.shootsGravitaxis.peek(),

            woodGrowthTime: this.woodGrowthTime.peek(),
            woodGrowthTimeVar: this.woodGrowthTimeVar.peek(),

            //leafLevel: this.leafLevel.peek(),
            leafLength: this.leafLength.peek(),
            leafLengthVar: this.leafLengthVar.peek(),
            leafRadius: this.leafRadius.peek(),
            leafRadiusVar: this.leafRadiusVar.peek(),
            leafGrowthTime: this.leafGrowthTime.peek(),
            leafGrowthTimeVar: this.leafGrowthTimeVar.peek(),
            leafPitch: this.leafPitchDeg.peek() * DegToRad,
            leafPitchVar: this.leafPitchDegVar.peek() * DegToRad,

            petioleLength: this.petioleLength.peek(),
            petioleLengthVar: this.petioleLengthVar.peek(),
            petioleRadius: this.petioleRadius.peek(),
            petioleRadiusVar: this.petioleRadiusVar.peek(),

            rootsDensity: this.rootsDensity.peek(),
            rootsGravitaxis: this.rootsGravitaxis.peek(),

            includeInRndGen: this.includeInRndGen.peek(),
        };
    }

    public loadPredefined(entry: { name: string; aka?: string; graphs: NamedGraph[]; configuration?: Parameters<typeof fromWireEntries>[0] }) {
        this.name.value = entry.name;
        this.aka.value = entry.aka ?? "";
        if (Array.isArray(entry.graphs) && entry.graphs.length > 0)
            this.behaviorGraphs.value = structuredClone(entry.graphs);
        else
            this.behaviorGraphs.value = [createDefaultNamedGraph("Main")];
        this.behaviorConfiguration.value = fromWireEntries(entry.configuration);
        applyBehaviorConfigurationToMorphology(this, this.behaviorConfiguration.peek());
        return this;
    }

    public load(s: any) {
        this.name.value = s.name;
        this.aka.value = s.aka;
        this.behaviorIndex.value = s.behavior ?? 0;
        if (Array.isArray(s.graphs) && s.graphs.length > 0)
            this.behaviorGraphs.value = structuredClone(s.graphs);
        if (Array.isArray(s.configuration))
            this.behaviorConfiguration.value = fromWireEntries(s.configuration);
        this.height.value = s.height;
        this.nodeDistance.value = s.nodeDistance;
        this.nodeDistanceVar.value = s.nodeDistanceVar;

        this.monopodialFactor.value = s.monopodialFactor;
        this.dominanceFactor.value = s.dominanceFactor;

        this.auxinsProduction.value = s.auxinsProduction;
        this.auxinsReach.value = s.auxinsReach;

        this.lateralsPerNode.value = s.lateralsPerNode;
        this.lateralRollDeg.value = s.lateralRoll * RadToDeg;
        this.lateralRollDegVar.value = s.lateralRollVar * RadToDeg;
        this.lateralPitchDeg.value = s.lateralPitch * RadToDeg;
        this.lateralPitchDegVar.value = s.lateralPitchVar * RadToDeg;

        this.twigsBending.value = s.twigsBending;
        this.twigsBendingApical.value = s.twigsBendingApical;
        this.bendingByLevel.value = s.twigsBendingLevel;
        this.shootsGravitaxis.value = s.shootsGravitaxis;

        this.woodGrowthTime.value = s.woodGrowthTime;
        this.woodGrowthTimeVar.value = s.woodGrowthTimeVar;

        //this.leafLevel.value = s.leafLevel;
        this.leafLength.value = s.leafLength;
        this.leafLengthVar.value = s.leafLengthVar;
        this.leafRadius.value = s.leafRadius;
        this.leafRadiusVar.value = s.leafRadiusVar;
        this.leafGrowthTime.value = s.leafGrowthTime;
        this.leafGrowthTimeVar.value = s.leafGrowthTimeVar;
        this.leafPitchDeg.value = s.leafPitch * RadToDeg;
        this.leafPitchDegVar.value = s.leafPitchVar * RadToDeg;

        this.petioleLength.value = s.petioleLength;
        this.petioleLengthVar.value = s.petioleLengthVar;
        this.petioleRadius.value = s.petioleRadius;
        this.petioleRadiusVar.value = s.petioleRadiusVar;

        this.rootsDensity.value = s.rootsDensity;
        this.rootsGravitaxis.value = s.rootsGravitaxis;

        this.includeInRndGen.value = s.hasOwnProperty('includeInRndGen') ? s.includeInRndGen : true;
        return this;
    }

    public serialize() {
        return {
            Name: this.name.peek(),
            Aka: this.aka.peek(),
            Behavior: this.behaviorIndex.peek(),

            Height: this.height.peek(),
            NodeDistance: this.nodeDistance.peek(),
            NodeDistanceVar: this.nodeDistanceVar.peek(),

            MonopodialFactor: this.monopodialFactor.peek(),
            DominanceFactor: this.dominanceFactor.peek(),

            AuxinsProduction: this.auxinsProduction.peek(),
            AusinsReach: this.auxinsReach.peek(),

            LateralsPerNode: this.lateralsPerNode.peek(),
            LateralRoll: this.lateralRollDeg.peek() * DegToRad,
            LateralRollVar: this.lateralRollDegVar.peek() * DegToRad,
            LateralPitch: this.lateralPitchDeg.peek() * DegToRad,
            LateralPitchVar: this.lateralPitchDegVar.peek() * DegToRad,

            TwigsBending: this.twigsBending.peek(),
            TwigsBendingLevel: this.bendingByLevel.peek(),
            TwigsBendingApical: this.twigsBendingApical.peek(),
            ShootsGravitaxis: this.shootsGravitaxis.peek(),

            WoodGrowthTime: this.woodGrowthTime.peek(),
            WoodGrowthTimeVar: this.woodGrowthTimeVar.peek(),

            //LeafLevel: this.leafLevel.peek(),
            LeafLength: this.leafLength.peek(),
            LeafLengthVar: this.leafLengthVar.peek(),
            LeafRadius: this.leafRadius.peek(),
            LeafRadiusVar: this.leafRadiusVar.peek(),
            LeafGrowthTime: this.leafGrowthTime.peek(),
            LeafGrowthTimeVar: this.leafGrowthTimeVar.peek(),
            LeafPitch: this.leafPitchDeg.peek() * DegToRad,
            LeafPitchVar: this.leafPitchDegVar.peek() * DegToRad,

            PetioleLength: this.petioleLength.peek(),
            PetioleLengthVar: this.petioleLengthVar.peek(),
            PetioleRadius: this.petioleRadius.peek(),
            PetioleRadiusVar: this.petioleRadiusVar.peek(),

            RootsSparsity: 100 - 99.999 * Math.min(1, Math.max(0, this.rootsDensity.peek())), //roots sparsity
            RootsGravitaxis: this.rootsGravitaxis.peek(),

            IncludeInRndGen: this.includeInRndGen.peek(),

            ...configBackedSpeciesSettings(this.behaviorConfiguration.peek()),
        };
    }
}