using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWPF.Models
{
	public class AttributeModel
	{
		public string Name { get; set; }

		public AttributeModel(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return Name;
		}
	}
	

}
