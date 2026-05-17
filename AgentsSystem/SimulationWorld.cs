namespace AgentsSystem;
public class SimulationWorld
{
	internal readonly List<IFormation> Formations = [];
	internal readonly List<Action<SimulationWorld, uint, IList<IFormation>, IList<IObstacle>>> Callbacks = [];
	public uint Timestep { get; private set; } = 0U;

	public int Count => Formations.Count;

	internal readonly List<IObstacle> Obstacles = [];

	public void ForEach(Action<IFormation> action) => Formations.ForEach(formation => action(formation));

    public void Add(IFormation formation) => Formations.Add(formation);

    public void AddRange(IEnumerable<IFormation> formations) => Formations.AddRange(formations);

    public void Add(IObstacle obstacle) => Obstacles.Add(obstacle);

    public void Run(uint simulationLength, Action<SimulationWorld, uint>? afterStep = null)
	{
		#if !DEBUG
		if (Formations.Count > 1 && afterStep == null)
			RunParallel(simulationLength);
		else
		#endif
			RunSequential(simulationLength, afterStep);
	}

	public void RunSequential(uint simulationLength, Action<SimulationWorld, uint>? afterStep = null)
	{
		for(int i = 0; i < simulationLength; ++i)
		{
			TickSequential();
			DeliverPostSequential();

			CensusSequential();
			ExecCallbacks();
			afterStep?.Invoke(this, Timestep);
			++Timestep;
		}
	}

	public void RunParallel(uint simulationLength)
	{
		for(int i = 0; i < simulationLength; ++i, ++Timestep)
		{
			TickParallel();
			DeliverPostParallel();

			CensusParallel();
			ExecCallbacks();
		}
	}

	void CensusSequential()
	{
		for(int i = 0; i < Formations.Count; ++i)
			Formations[i].Census();
	}

	void CensusParallel() => Parallel.For(0, Formations.Count, i => Formations[i].Census());

	void TickSequential()
	{
		//Debug.WriteLine($"TIMESTEP: {Timestep}");
		for(int i = 0; i < Formations.Count; ++i)
			Formations[i].Tick(Timestep);
	}

	void TickParallel() => Parallel.For(0, Formations.Count, i => Formations[i].Tick(Timestep));

	public void DeliverPostSequential()
	{
		var anyDelivered = true;
		while(anyDelivered)
		{
			anyDelivered = false;
			for(int i = 0; i < Formations.Count; ++i)
				if (Formations[i].HasUndeliveredPost)
				{
					Formations[i].DeliverPost(Timestep);
					anyDelivered = true;
				}
		}
	}

	public void DeliverPostParallel()
	{
		var anyDelivered = true;
		while(anyDelivered)
		{
			anyDelivered = false;
			Parallel.For(0, Formations.Count, i => {
				if (Formations[i].HasUndeliveredPost)
				{
					Formations[i].DeliverPost(Timestep);
					anyDelivered = true;
				}
			});
		}
	}

	public void AddCallback(Action<SimulationWorld, uint, IList<IFormation>, IList<IObstacle>> callback) => Callbacks.Add(callback);

	public void ExecCallbacks()
	{
		foreach(var callback in Callbacks)
			callback(this, Timestep, Formations, Obstacles);
	}

	public string ToJson()
	{
		var sb = new System.Text.StringBuilder();
		//assuming all formations are present all the time (no additions or removals)
		sb.Append("{ \"Formations\": [ ");
		for(int i = 0; i < Formations.Count; ++i)
		{
			sb.Append(Utils.Export.Json(Formations[i]));
			if (i < Formations.Count - 1)
				sb.Append(", ");
		}
		sb.Append("]}");

		return sb.ToString();
	}

	public Func<byte, List<IFormation>, List<IObstacle>, byte[]> StreamExporterFunc = null;
	public byte[]? ExportToStream(byte version) => StreamExporterFunc == null ? null : StreamExporterFunc(version, Formations, Obstacles);
	public string RendererName;
}
