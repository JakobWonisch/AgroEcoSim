import type { BehaviorConfigEntry } from './behaviorConfiguration';

const RadToDeg = 180.0 / Math.PI;

/** Stable config ids from bootstrap builders — keep in sync with DefaultSpeciesGraphBuilder.ConfigIds. */
export const ConfigIds = {
    height: 'default-config-height',
    woodGrowthTime: 'default-config-wood-growth-time',
    woodGrowthTimeVar: 'default-config-wood-growth-time-var',
    lateralsPerNode: 'default-config-laterals-per-node',
    leafLength: 'default-config-leaf-length',
    leafLengthVar: 'default-config-leaf-length-var',
    leafRadius: 'default-config-leaf-radius',
    leafRadiusVar: 'default-config-leaf-radius-var',
    leafGrowthTime: 'default-config-leaf-growth-time',
    leafGrowthTimeVar: 'default-config-leaf-growth-time-var',
    petioleLength: 'default-config-petiole-length',
    petioleLengthVar: 'default-config-petiole-length-var',
    petioleRadius: 'default-config-petiole-radius',
    petioleRadiusVar: 'default-config-petiole-radius-var',
    nodeDistance: 'default-config-node-distance',
    nodeDistanceVar: 'default-config-node-distance-var',
    monopodialFactor: 'default-config-monopodial-factor',
    dominanceFactor: 'default-config-dominance-factor',
    auxinsProduction: 'default-config-auxins-production',
    lateralRoll: 'default-config-lateral-roll',
    lateralRollVar: 'default-config-lateral-roll-var',
    lateralPitch: 'default-config-lateral-pitch',
    lateralPitchVar: 'default-config-lateral-pitch-var',
    leafPitch: 'default-config-leaf-pitch',
    leafPitchVar: 'default-config-leaf-pitch-var',
    twigsBending: 'default-config-twig-bending',
    twigsBendingLevel: 'default-config-twig-bending-level',
    twigsBendingApical: 'default-config-twig-bending-apical',
    shootsGravitaxis: 'default-config-shoots-gravitaxis',
    rizomeLength: 'default-config-rizome-length',
    rizomeRadius: 'default-config-rizome-radius',
} as const;

function configNumber(entries: BehaviorConfigEntry[], id: string): number | undefined {
    const entry = entries.find(e => e.id === id);
    if (!entry || entry.type !== 'number' || typeof entry.value !== 'number')
        return undefined;
    return entry.value;
}

/** Mirror behaviorConfiguration into legacy Species signals for HUD display consistency. */
export function syncSpeciesSignalsFromConfiguration(
    species: {
        height: { value: number };
        woodGrowthTime: { value: number };
        woodGrowthTimeVar: { value: number };
        lateralsPerNode: { value: number };
        leafLength: { value: number };
        leafLengthVar: { value: number };
        leafRadius: { value: number };
        leafRadiusVar: { value: number };
        leafGrowthTime: { value: number };
        leafGrowthTimeVar: { value: number };
        petioleLength: { value: number };
        petioleLengthVar: { value: number };
        petioleRadius: { value: number };
        petioleRadiusVar: { value: number };
        nodeDistance: { value: number };
        nodeDistanceVar: { value: number };
        monopodialFactor: { value: number };
        dominanceFactor: { value: number };
        auxinsProduction: { value: number };
        lateralRollDeg: { value: number };
        lateralRollDegVar: { value: number };
        lateralPitchDeg: { value: number };
        lateralPitchDegVar: { value: number };
        leafPitchDeg: { value: number };
        leafPitchDegVar: { value: number };
        twigsBending: { value: number };
        twigsBendingApical: { value: number };
        bendingByLevel: { value: number };
        shootsGravitaxis: { value: number };
    },
    entries: BehaviorConfigEntry[],
) {
    const n = (id: string) => configNumber(entries, id);
    const set = (id: string, apply: (v: number) => void) => {
        const v = n(id);
        if (v !== undefined) apply(v);
    };

    set(ConfigIds.height, v => { species.height.value = v; });
    set(ConfigIds.woodGrowthTime, v => { species.woodGrowthTime.value = v; });
    set(ConfigIds.woodGrowthTimeVar, v => { species.woodGrowthTimeVar.value = v; });
    set(ConfigIds.lateralsPerNode, v => { species.lateralsPerNode.value = Math.round(v); });
    set(ConfigIds.leafLength, v => { species.leafLength.value = v; });
    set(ConfigIds.leafLengthVar, v => { species.leafLengthVar.value = v; });
    set(ConfigIds.leafRadius, v => { species.leafRadius.value = v; });
    set(ConfigIds.leafRadiusVar, v => { species.leafRadiusVar.value = v; });
    set(ConfigIds.leafGrowthTime, v => { species.leafGrowthTime.value = v; });
    set(ConfigIds.leafGrowthTimeVar, v => { species.leafGrowthTimeVar.value = v; });
    set(ConfigIds.petioleLength, v => { species.petioleLength.value = v; });
    set(ConfigIds.petioleLengthVar, v => { species.petioleLengthVar.value = v; });
    set(ConfigIds.petioleRadius, v => { species.petioleRadius.value = v; });
    set(ConfigIds.petioleRadiusVar, v => { species.petioleRadiusVar.value = v; });
    set(ConfigIds.nodeDistance, v => { species.nodeDistance.value = v; });
    set(ConfigIds.nodeDistanceVar, v => { species.nodeDistanceVar.value = v; });
    set(ConfigIds.monopodialFactor, v => { species.monopodialFactor.value = v; });
    set(ConfigIds.dominanceFactor, v => { species.dominanceFactor.value = v; });
    set(ConfigIds.auxinsProduction, v => { species.auxinsProduction.value = v; });
    set(ConfigIds.lateralRoll, v => { species.lateralRollDeg.value = v * RadToDeg; });
    set(ConfigIds.lateralRollVar, v => { species.lateralRollDegVar.value = v * RadToDeg; });
    set(ConfigIds.lateralPitch, v => { species.lateralPitchDeg.value = v * RadToDeg; });
    set(ConfigIds.lateralPitchVar, v => { species.lateralPitchDegVar.value = v * RadToDeg; });
    set(ConfigIds.leafPitch, v => { species.leafPitchDeg.value = v * RadToDeg; });
    set(ConfigIds.leafPitchVar, v => { species.leafPitchDegVar.value = v * RadToDeg; });
    set(ConfigIds.twigsBending, v => { species.twigsBending.value = v; });
    set(ConfigIds.twigsBendingApical, v => { species.twigsBendingApical.value = v; });
    set(ConfigIds.twigsBendingLevel, v => { species.bendingByLevel.value = v; });
    set(ConfigIds.shootsGravitaxis, v => { species.shootsGravitaxis.value = v; });
}
