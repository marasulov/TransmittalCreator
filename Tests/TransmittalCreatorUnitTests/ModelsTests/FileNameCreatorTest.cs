using NUnit.Framework;
using TransmittalCreator.ViewModels;
using TransmittalCreator.ViewModels.ViewModels;

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
