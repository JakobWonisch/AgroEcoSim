import { hasCycle, wouldCreateCycle } from '../src/components/hud/nodes/graphValidation';

describe('wouldCreateCycle', () => {
    test('allows acyclic connection', () => {
        const existing = [{ source: 'a', target: 'b' }, { source: 'b', target: 'c' }];
        expect(wouldCreateCycle(existing, 'c', 'd')).toBe(false);
    });

    test('blocks connection that closes a cycle', () => {
        const existing = [{ source: 'a', target: 'b' }, { source: 'b', target: 'c' }];
        expect(wouldCreateCycle(existing, 'c', 'a')).toBe(true);
    });

    test('blocks self-loop', () => {
        expect(wouldCreateCycle([], 'a', 'a')).toBe(true);
    });

    test('allows replacement edge when no cycle would form', () => {
        const existing = [{ source: 'a', target: 'b' }, { source: 'b', target: 'c' }];
        expect(wouldCreateCycle(existing, 'b', 'c')).toBe(false);
    });

    test('blocks 2-node mutual dependency (Not loop)', () => {
        const existing = [{ source: 'a', target: 'b' }];
        expect(wouldCreateCycle(existing, 'b', 'a')).toBe(true);
    });
});

describe('hasCycle', () => {
    test('returns false for acyclic graph', () => {
        const nodeIds = ['a', 'b', 'c'];
        const connections = [{ source: 'a', target: 'b' }, { source: 'b', target: 'c' }];
        expect(hasCycle(nodeIds, connections)).toBe(false);
    });

    test('returns true for 2-node Not loop', () => {
        const nodeIds = ['a', 'b'];
        const connections = [{ source: 'a', target: 'b' }, { source: 'b', target: 'a' }];
        expect(hasCycle(nodeIds, connections)).toBe(true);
    });

    test('returns true for self-loop', () => {
        const nodeIds = ['a'];
        const connections = [{ source: 'a', target: 'a' }];
        expect(hasCycle(nodeIds, connections)).toBe(true);
    });
});
