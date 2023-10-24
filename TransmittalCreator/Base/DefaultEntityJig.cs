using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace TransmittalCreator.Base;

/// <summary>
/// Реализация стандартной EntityJig
/// </summary>
public class DefaultEntityJig : EntityJig
{
	public DefaultEntityJig(Entity entity) : base(entity)
	{

	}

	protected override SamplerStatus Sampler(JigPrompts prompts)
	{
		throw new System.NotImplementedException();
	}

	protected override bool Update()
	{
		throw new System.NotImplementedException();
	}
}