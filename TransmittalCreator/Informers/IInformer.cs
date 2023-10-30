using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransmittalCreator.Informers
{
	/// <summary>
	/// Description of IInformer.
	/// </summary>
	public interface IInformer
	{
		void Notify(string message);
		void Alert(string message);
		void Warning(string message);
		void Log(string message);
	}
}
