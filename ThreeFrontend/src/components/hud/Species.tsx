import { h, Fragment } from "preact";
import appstate from "../../appstate";
import { Species } from "src/helpers/Species";
import { computed, signal, useSignal } from "@preact/signals";
import BehaviorEditor from "./nodes/BehaviorEditor";
import SpeciesConfigurationEditor from "./SpeciesConfigurationEditor";
import type { NamedGraph } from "./nodes/Conversion";
import { createDefaultNamedGraph } from "./nodes/Conversion";

const conflictStyle: h.JSX.CSSProperties = {
  borderColor: "#ee2211",
  borderStyle: "solid",
};

const selectedSpecies = signal("");

/** Per-species selected behavior graph id (left list). */
const selectedGraphIdBySpecies = signal<Map<string, string>>(new Map());

type SpeciesSidebarView = "configuration" | "graphs";
const speciesSidebarViewBySpecies = signal<Map<string, SpeciesSidebarView>>(
  new Map(),
);

function getSidebarView(speciesName: string): SpeciesSidebarView {
  return speciesSidebarViewBySpecies.peek().get(speciesName) ?? "graphs";
}

function setSidebarView(speciesName: string, view: SpeciesSidebarView) {
  const next = new Map(speciesSidebarViewBySpecies.peek());
  next.set(speciesName, view);
  speciesSidebarViewBySpecies.value = next;
}

function getSelectedGraphId(speciesName: string, graphs: NamedGraph[]): string {
  const m = selectedGraphIdBySpecies.peek();
  let id = m.get(speciesName);
  if (!id || !graphs.some((g) => g.id === id)) {
    id = graphs[0]?.id ?? "";
    const next = new Map(m);
    next.set(speciesName, id);
    selectedGraphIdBySpecies.value = next;
  }
  return id;
}

function setSelectedGraphId(speciesName: string, id: string) {
  const next = new Map(selectedGraphIdBySpecies.peek());
  next.set(speciesName, id);
  selectedGraphIdBySpecies.value = next;
}

export function SpeciesList() {
  return (
    <div
      className="stack"
      style={{
        height: "100%",
        width: "100%",
        minWidth: 0,
      }}
    >
      <select
        onChange={(e) =>
          (selectedSpecies.value =
            e.target[(e.target as HTMLSelectElement).selectedIndex].title)
        }
        style={{
          alignSelf: "start",
        }}
      >
        <option></option>
        {appstate.species.value.map((x) => (
          <option title={x.name}>
            {x.name}
            {x.aka.value?.length > 0 ? ` (${x.aka})` : ""}
          </option>
        ))}
      </select>
      <button
        name="species"
        onClick={() => appstate.pushRndSpecies()}
        style={{
          alignSelf: "start",
        }}
      >
        Add new species
      </button>

      {/* <ul>
            {appstate.species.value.map((x: Species, i) => <SpeciesItem species={x} index={i}/>)}
        </ul> */}
      {selectedSpecies.value?.length > 0 ? <SpeciesItem></SpeciesItem> : <></>}
    </div>
  );
}

const dominanceFactorTooltip =
  "Reduces the growth of lateral branches. Multiplies with each recursion level.";
const auxinsProductionTooltip =
  "Each meristem node generates this amount of auxins (given in unspecified units).";
const auxinsReachTooltip =
  "Auxines propagate this far within the plant with a linear falloff.";
const maxLeafLevelTooltip =
  "Limits the level of branches that support petioles. Technically it coresponds to the maximum possible level of descendants.";

export function SpeciesItem() {
  const inputList = appstate.species.value;
  const index = inputList.findIndex(
    (x) => x.name.value == selectedSpecies.value,
  );
  if (index < 0) return <></>;
  const species = inputList[index];

  const nameConflict = useSignal(false);
  const links = computed(() =>
    appstate.seeds.value.reduce(
      (a, c) => a + (c.species.value == species.name.value ? 1 : 0),
      0,
    ),
  );
  const graphs = species.behaviorGraphs.value;
  void selectedGraphIdBySpecies.value;
  void speciesSidebarViewBySpecies.value;
  const sidebarView = getSidebarView(species.name.value);
  const selectedGraphId = getSelectedGraphId(species.name.value, graphs);
  const selectedGraph =
    graphs.find((g) => g.id === selectedGraphId) ?? graphs[0];

  return (
    <div
      class="speciesDetails stack"
      style={{
        gap: "1em",
        flex: 1,
        minHeight: 0,
        width: "100%",
        maxWidth: "100%",
      }}
    >
      <div class="inputs">
        <div>
          <input
            type="text"
            name={`name-${index}`}
            value={species.name.value}
            title={"Name of the species"}
            style={nameConflict.value ? conflictStyle : null}
            class="speciesNameInput"
            onChange={(e) => {
              const name = e.currentTarget.value;
              if (
                appstate.species.value.some(
                  (s, i) => i !== index && s.name.value == name,
                )
              ) {
                nameConflict.value = true;
                e.currentTarget.value = species.name.value;
                setTimeout(() => (nameConflict.value = false), 2000);
              } else {
                species.name.value = name;
                nameConflict.value = false;
              }
            }}
          />
          <label for={`name-${index}`}>
            Name{" "}
            {nameConflict.value ? (
              <span style={{ color: "#ee2211" }}>conflict!</span>
            ) : (
              <></>
            )}
          </label>
          <button
            style={{ float: "right" }}
            onClick={() => appstate.removeSpeciesAt(index)}
            disabled={links.value > 0 || appstate.species.value.length <= 1}
          >
            🗙
          </button>
          <span style={{ float: "right", marginRight: "0.5em" }}>
            🔗 {links.value}
          </span>
          <label for={`aka-${index}`} title={"Colloquial name"}>
            aka
          </label>
          <input
            type="text"
            name={`aka-${index}`}
            value={species.aka.value ?? ""}
            title={"Colloquial name"}
            class="speciesAkaInput"
            onChange={(e) => (species.aka.value = e.currentTarget.value)}
          />
        </div>
      </div>

      {selectedGraph ? (
        <div
          style={{
            display: "flex",
            flexDirection: "row",
            flex: 1,
            minHeight: 0,
            gap: "0.75em",
            width: "100%",
          }}
        >
          <div
            style={{
              width: 220,
              flexShrink: 0,
              display: "flex",
              flexDirection: "column",
              gap: 6,
              overflow: "auto",
              border: "1px solid rgba(255,255,255,0.15)",
              borderRadius: 4,
              padding: 8,
            }}
          >
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                marginBottom: "0.75em",
              }}
            >
              <strong>Configuration</strong>
              <button
                type="button"
                title="Edit configuration values"
                onClick={() =>
                  setSidebarView(species.name.value, "configuration")
                }
              >
                edit
              </button>
            </div>
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
              }}
            >
              <strong>Graphs</strong>
              <button
                type="button"
                onClick={() => {
                  const list = species.behaviorGraphs.peek();
                  const n = list.length + 1;
                  const entry = createDefaultNamedGraph(`Graph ${n}`);
                  species.behaviorGraphs.value = [...list, entry];
                  setSelectedGraphId(species.name.value, entry.id);
                }}
              >
                +
              </button>
            </div>
            {graphs.map((g, gi) => (
              <div
                key={g.id}
                onClick={() => {
                  setSidebarView(species.name.value, "graphs");
                  setSelectedGraphId(species.name.value, g.id);
                }}
                style={{
                  display: "flex",
                  flexDirection: "column",
                  gap: 4,
                  padding: 6,
                  cursor: "pointer",
                  background:
                    sidebarView === "graphs" && g.id === selectedGraphId
                      ? "rgba(80,160,120,0.25)"
                      : "rgba(0,0,0,0.2)",
                  borderRadius: 4,
                  border:
                    sidebarView === "graphs" && g.id === selectedGraphId
                      ? "2px solid #5a8"
                      : "1px solid rgba(255,255,255,0.12)",
                }}
              >
                <input
                  type="text"
                  value={g.name}
                  onClick={(e) => e.stopPropagation()}
                  onInput={(e) => {
                    const v = (e.target as HTMLInputElement).value;
                    species.behaviorGraphs.value = species.behaviorGraphs
                      .peek()
                      .map((x) => (x.id === g.id ? { ...x, name: v } : x));
                  }}
                  style={{ width: "100%", boxSizing: "border-box" }}
                />
                <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                  <button
                    type="button"
                    disabled={gi === 0}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (gi === 0) return;
                      const list = [...species.behaviorGraphs.peek()];
                      [list[gi - 1], list[gi]] = [list[gi], list[gi - 1]];
                      species.behaviorGraphs.value = list;
                    }}
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    disabled={gi >= graphs.length - 1}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (gi >= graphs.length - 1) return;
                      const list = [...species.behaviorGraphs.peek()];
                      [list[gi], list[gi + 1]] = [list[gi + 1], list[gi]];
                      species.behaviorGraphs.value = list;
                    }}
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    disabled={graphs.length <= 1}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (graphs.length <= 1) return;
                      const list = species.behaviorGraphs
                        .peek()
                        .filter((x) => x.id !== g.id);
                      species.behaviorGraphs.value = list;
                      if (selectedGraphId === g.id)
                        setSelectedGraphId(species.name.value, list[0].id);
                    }}
                  >
                    Remove
                  </button>
                </div>
              </div>
            ))}
          </div>
          <div
            style={{
              flex: 1,
              minWidth: 0,
              minHeight: 360,
              display: "flex",
              flexDirection: "column",
              alignItems:
                sidebarView === "configuration" ? "flex-start" : "stretch",
            }}
          >
            {sidebarView === "configuration" ? (
              <SpeciesConfigurationEditor species={species} />
            ) : (
              <BehaviorEditor
                key={`${species.name.value}:${selectedGraph.id}`}
                species={species}
                namedGraph={selectedGraph}
              />
            )}
          </div>
        </div>
      ) : (
        <></>
      )}
    </div>
  );
}
