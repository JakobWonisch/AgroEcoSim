# Proposed Framework for Node-based Species Programming

The current structure of the `Tick` method in agents is too long to represent in nodes directly. This many instructions would only lead to an incomprehensible node forrest. Therefore I propose breaking up the different intended behaviors into "paths".

Each path consists of one collection of nodes representing one possible agent configuration (for example: `agent.Organ == OrganTypes.Leaf && agent.Water_g > 0f`).

Behaviors can therefore be worked on in separate node clusters of managable size.

## General Structure

Each "path" can use `input nodes` that provide values at the start of each tick such as organ type, current phase or current `lifeSupportPerHour`.
These values can be processed using a set of general, `intermediate nodes` such as an `if greater than/equal to/less than` or a `add/subtract/multiply/divide` node.
Finally, different actions can be triggered using `output nodes` such as a `create bud` or `add to water_g` node.

## Data Types

To keep the set of intermediate nodes managable all values shall have one of the following types:

- boolean
- numeric

All input nodes must provide values of one of these types and all output nodes must only consume these types. Only connection points of the same type may be connected together. Conversions between types must be done explicitly using for example `greater than` or `if/else`.

## Input Nodes

To be able to match any of the currently configured behaviors the inputs must provide boolean values for each agent configuration. At a minimum the input nodes must contain the following nodes:

- agent organ: gives a boolean output for each organ type that is true only when the agent has that type (can of course be negated using `not` intermediate nodes)
- formation phase: gives a boolean output for each formation phase
- (if decided to keep around) species settings: provides a numeric or boolean output where approprieate for each species setting
- constant numeric/boolean values defined by the user

## Intermediate Nodes

The highest level of flexibility can be guaranteed by providing users with the most common constructs in traditional programming languages. Therefore the following nodes must be included as intermediate nodes:

- boolean not/and/or/xor
- add/subtract/multiply/divide
- greater than/less than/equal to/geq/leq
- if-else

## Output Nodes

Finally, triggering any actions/setting values is done using output nodes. This way a bud can turn into a new stem or a leaf or whatever else the author desires and agent values can be updated. The following output nodes must be provided:

- createRhizome
- createLeaves
- addWater_g
- addLength
- addRadius

A special output node is `isActive`. Each path can set itself active or inactive by passing the value to this output node. This controls whether the rest of the path's node tree is processed at all. Together with the organ input/if-else-logic it is equivalent to the if-checks/switch-blocks in the current CS implementation. There may only be one `isActive` node per path to avoid conflicting values. If there is none it is considered always active.

## Species Settings

Since some values may need to be reused in multiple paths the concept of species settings still could make sense in the node based behavior system. Alternatively users may be able to define their own constants using a special input node `Species Constant`. In this node they can type an arbitrary name that will then show up in the GUI as a settable numeric value. All species constant nodes that share the name will provide the same value in the node tree in all paths.
A mixture of approaches is also possible. Providing one input node with all species settings and the ability to define custom fields using the species constant node.

## Open Questions

- In some cases I think I saw that the order of operations matters for the value of `Water_g` for example. Is this something we want to keep or should the execution of different paths be order independent?
If order should be preserved the order of paths can of course be made to be user sortable.

- In principle this could also include underground agents. is this something that should be implemented?

- How should we validate that this new system can recreate the CS implementation of the existing agents? Is there some kind of test imaginable where we can for example deterministically grow a plant for X steps using both behaviors and then compare the states?