using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TransmittalCreator.ViewModels;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreatorUnitTests.ViewModelTests
{
	[TestFixture]
	public class CreatePdfVmTest
	{
		private ICreatePdfVm _createPdfVm;

		[SetUp]
		public void SetUp()
		{
			_createPdfVm = new CreatePdfVm();
		}

		

	}
}
