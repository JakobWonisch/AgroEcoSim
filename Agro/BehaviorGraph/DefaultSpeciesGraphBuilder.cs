using System.Text.Json;

namespace Agro.BehaviorGraph;

/// <summary>
/// Bootstrap behavior graph for the Default species — best-effort wiring of
/// <see cref="AboveGroundAgent.TickDefault"/> using only the public node library.
/// </summary>
public static class DefaultSpeciesGraphBuilder
{
	public static global::ExportedGraph Build() => new GraphBuilder().Build();

	sealed class GraphBuilder
	{
		readonly List<global::GraphNode> _nodes = [];
		readonly List<global::GraphConnection> _connections = [];
		int _connSeq;

		public global::ExportedGraph Build()
		{
			BuildGate();
			BuildPhotosynthesis(yBase: 80);
			BuildEnergyDepletion(yBase: 420);
			BuildMeristemAuxins(yBase: 620);
			return new global::ExportedGraph { Nodes = _nodes, Connections = _connections };
		}

		void BuildGate()
		{
			var gateBool = AddBool("gate-bool", true, 0, 0);
			var active = Add("gate-active", "Active", 200, 0);
			Connect(gateBool, "bool", active, "isActive");
		}

		void BuildPhotosynthesis(float yBase)
		{
			var organ = Add("photo-organ", "Agent Type Input", 0, yBase);
			var state = Add("photo-state", "Agent State Input", 0, yBase + 60);
			var ir = Add("photo-ir", "Irradiance Input", 0, yBase + 120);

			var c0 = AddNum("photo-c0", 0f, 280, yBase);
			var c001 = AddNum("photo-c001", 0.01f, 280, yBase + 40);
			var c005 = AddNum("photo-c005", 0.005f, 280, yBase + 80);
			var c2 = AddNum("photo-c2", 2f, 280, yBase + 120);

			var hasWater = Add("photo-has-water", "Greater Than (or Equal)", 520, yBase);
			var bright = Add("photo-bright", "Greater Than (or Equal)", 520, yBase + 50);
			var canPhoto2 = Add("photo-can2", "And", 760, yBase + 70);
			var canPhoto = Add("photo-can", "And", 1000, yBase + 20);

			var surface = Add("photo-surface", "Multiply", 520, yBase + 120);
			var surface2 = Add("photo-surface2", "Multiply", 760, yBase + 120);
			var lightGain = Add("photo-light", "Multiply", 1000, yBase + 120);
			var lightGain2 = Add("photo-light2", "Multiply", 1240, yBase + 80);
			var lightLtWater = Add("photo-min-cmp", "Less Than (or Equal)", 1240, yBase + 180);
			var photoAmt = Add("photo-amt", "If / Else", 1480, yBase + 140);

			var gatedPhoto = Add("photo-gated", "If / Else", 1720, yBase + 140);
			var negPhoto = Add("photo-neg", "Subtract", 1720, yBase + 200);
			var gatedNeg = Add("photo-gated-neg", "If / Else", 1960, yBase + 200);
			var prodInv = Add("photo-prod", "Divide", 1720, yBase + 260);
			var gatedProd = Add("photo-gated-prod", "If / Else", 1960, yBase + 260);

			var dEnergy = Add("photo-dE", "Delta Energy", 2200, yBase + 140);
			var dWater = Add("photo-dW", "Delta Water", 2200, yBase + 200);
			var accProd = Add("photo-acc", "Accumulate Production", 2200, yBase + 260);

			Connect(state, "water", hasWater, "a");
			Connect(c0, "num", hasWater, "b");
			Connect(ir, "irradiance", bright, "a");
			Connect(c001, "num", bright, "b");

			Connect(hasWater, "out", canPhoto2, "a");
			Connect(bright, "out", canPhoto2, "b");
			Connect(organ, "leaf", canPhoto, "a");
			Connect(canPhoto2, "out", canPhoto, "b");

			Connect(state, "length", surface, "a");
			Connect(state, "radius", surface, "b");
			Connect(surface, "out", surface2, "a");
			Connect(c2, "num", surface2, "b");
			Connect(surface2, "out", lightGain, "a");
			Connect(ir, "irradiance", lightGain, "b");
			Connect(lightGain, "out", lightGain2, "a");
			Connect(c005, "num", lightGain2, "b");

			Connect(lightGain2, "out", lightLtWater, "a");
			Connect(state, "water", lightLtWater, "b");
			Connect(lightLtWater, "out", photoAmt, "condition");
			Connect(lightGain2, "out", photoAmt, "trueValue");
			Connect(state, "water", photoAmt, "falseValue");

			Connect(canPhoto, "out", gatedPhoto, "condition");
			Connect(photoAmt, "out", gatedPhoto, "trueValue");
			Connect(c0, "num", gatedPhoto, "falseValue");

			Connect(c0, "num", negPhoto, "a");
			Connect(photoAmt, "out", negPhoto, "b");
			Connect(canPhoto, "out", gatedNeg, "condition");
			Connect(negPhoto, "out", gatedNeg, "trueValue");
			Connect(c0, "num", gatedNeg, "falseValue");

			Connect(photoAmt, "out", prodInv, "a");
			Connect(surface2, "out", prodInv, "b");
			Connect(canPhoto, "out", gatedProd, "condition");
			Connect(prodInv, "out", gatedProd, "trueValue");
			Connect(c0, "num", gatedProd, "falseValue");

			Connect(gatedPhoto, "out", dEnergy, "amount");
			Connect(gatedNeg, "out", dWater, "amount");
			Connect(gatedProd, "out", accProd, "amount");
		}

		void BuildEnergyDepletion(float yBase)
		{
			var organ = Add("die-organ", "Agent Type Input", 0, yBase);
			var state = Add("die-state", "Agent State Input", 0, yBase + 60);
			var c0 = AddNum("die-c0", 0f, 240, yBase);

			var starved = Add("die-starved", "Less Than (or Equal)", 480, yBase);
			Connect(state, "energy", starved, "a");
			Connect(c0, "num", starved, "b");

			var isPetiole = Add("die-petiole", "And", 720, yBase);
			var isLeaf = Add("die-leaf", "And", 720, yBase + 60);
			var notLeaf = Add("die-not-leaf", "Not", 480, yBase + 60);
			var notPetiole = Add("die-not-pet", "Not", 480, yBase + 120);
			var notLeafPet = Add("die-not-both", "And", 960, yBase + 90);
			var isOther = Add("die-other", "And", 1200, yBase + 120);

			Connect(starved, "out", isPetiole, "a");
			Connect(organ, "petiole", isPetiole, "b");
			Connect(starved, "out", isLeaf, "a");
			Connect(organ, "leaf", isLeaf, "b");

			Connect(organ, "leaf", notLeaf, "a");
			Connect(organ, "petiole", notPetiole, "a");
			Connect(notLeaf, "out", notLeafPet, "a");
			Connect(notPetiole, "out", notLeafPet, "b");
			Connect(starved, "out", isOther, "a");
			Connect(notLeafPet, "out", isOther, "b");

			var makeBud = Add("die-bud", "Make Bud", 1440, yBase);
			var death = Add("die-death", "Death", 1440, yBase + 60);
			var deathParent = Add("die-death-p", "Death Parent", 1440, yBase + 120);
			var deathOther = Add("die-death-o", "Death", 1440, yBase + 180);

			Connect(isPetiole, "out", makeBud, "trigger");
			Connect(isLeaf, "out", death, "trigger");
			Connect(isLeaf, "out", deathParent, "trigger");
			Connect(isOther, "out", deathOther, "trigger");
		}

		void BuildMeristemAuxins(float yBase)
		{
			var organ = Add("aux-organ", "Agent Type Input", 0, yBase);
			var c0 = AddNum("aux-c0", 0f, 240, yBase);
			var cAux = AddNum("aux-prod", 1f, 240, yBase + 50);
			var auxVal = Add("aux-val", "If / Else", 480, yBase + 20);
			var setAux = Add("aux-set", "Set Auxins", 720, yBase + 20);

			Connect(organ, "meristem", auxVal, "condition");
			Connect(cAux, "num", auxVal, "trueValue");
			Connect(c0, "num", auxVal, "falseValue");
			Connect(auxVal, "out", setAux, "value");
		}

		string Add(string id, string label, float x, float y, JsonElement? data = null)
		{
			_nodes.Add(new global::GraphNode
			{
				Id = id,
				Label = label,
				Data = data ?? JsonSerializer.SerializeToElement(new { }),
				Position = new global::NodePosition { X = x, Y = y },
			});
			return id;
		}

		string AddBool(string id, bool value, float x, float y) =>
			Add(id, "Boolean Input", x, y, JsonSerializer.SerializeToElement(new Dictionary<string, bool> { ["bool"] = value }));

		string AddNum(string id, float value, float x, float y) =>
			Add(id, "Number Input", x, y, JsonSerializer.SerializeToElement(new Dictionary<string, float> { ["value"] = value }));

		void Connect(string source, string sourceOutput, string target, string targetInput)
		{
			_connections.Add(new global::GraphConnection
			{
				Id = $"c-{_connSeq++}",
				Source = source,
				SourceOutput = sourceOutput,
				Target = target,
				TargetInput = targetInput,
			});
		}
	}
}
