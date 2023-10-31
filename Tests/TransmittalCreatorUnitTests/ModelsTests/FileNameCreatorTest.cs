using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models;
using TransmittalCreator.Services;
using TransmittalCreator.ViewModels;
using TransmittalCreator.ViewModels.ViewModels;
using TransmittalCreator.Infrastructure;

namespace TransmittalCreatorUnitTests.ModelsTests
{
	[TestFixture]
	public class FileNameCreatorTest 
	{
		private TransmittalCreator.Services.FileNameCreator _fileNameCreator;
		private ICreatePdfVm _createPdf;

		[SetUp]
		public void SetUp()
		{
			_createPdf = new CreatePdfVm();
			_createPdf.Prefix = "prefix";
			_createPdf.Suffix = "suffix";
			_createPdf.RenumberStartValue = "0";

			//_fileNameCreator = new TransmittalCreator.Services.FileNameCreator(_createPdf);
		}

		[Test]
		public void GetPrintModelsTest()
		{
			Assert.IsFalse(_createPdf.IsAttributeChecked);
			Assert.IsFalse(_createPdf.IsCheckedNumbering);
			Assert.AreEqual(_createPdf.RenumberStartValue, "0");
			Assert.IsTrue(_createPdf.IsSelectedOrder);
		}

	}
}
